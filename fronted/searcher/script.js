const searchInput = document.getElementById("searchInput")
const searchButton = document.getElementById("searchButton")
const videoGrid = document.getElementById("videoGrid")
const resultsInfo = document.getElementById("resultsInfo")
const noResults = document.getElementById("noResults")
const loading = document.getElementById("loading")
const errorMessage = document.getElementById("errorMessage")
const errorText = document.getElementById("errorText")
const videoPlayerSection = document.getElementById("videoPlayerSection")
const videoPlayer = document.getElementById("videoPlayer")
const currentVideoTitle = document.getElementById("currentVideoTitle")
const currentFormatInfo = document.getElementById("currentFormatInfo")
const currentFormat = document.getElementById("currentFormat")

const API_BASE_URL = "https://localhost:7167/api/Search/search"
const THUMBNAIL_URL =
  "https://rahool.blob.core.windows.net/utube/thumbnails/05517cf5-97d7-4b18-afd7-b206784c736f/thumb_019.jpg"

let currentVideos = []
let currentHls = null
let currentDash = null
let activeVideoItem = null
let selectedFormat = "hls"

const Hls = window.Hls
const dashjs = window.dashjs

function getSelectedFormat() {
  const formatRadios = document.querySelectorAll('input[name="format"]')
  for (const radio of formatRadios) {
    if (radio.checked) return radio.value
  }
  return "hls"
}

document.addEventListener("change", (e) => {
  if (e.target.name === "format") {
    selectedFormat = e.target.value
    console.log("Format changed to:", selectedFormat)

    const query = searchInput.value.trim()

    if (query) {
      videoPlayerSection.classList.add("hidden")
      searchVideos(query)
    }
  }
})

function renderVideos(videos) {
  hideAllSections()

  if (videos.length === 0) {
    noResults.classList.remove("hidden")
    return
  }
 console.log(videos)
  videoGrid.innerHTML = videos
    .map((video, index) => {
      const hasThumbnail = !!video.selectedImageName

      return `
      <div class="video-item" data-index="${index}">
        <div class="video-thumbnail">
          ${
            hasThumbnail
              ? `<img src="${video.selectedImageName}" 
                     alt="${escapeHtml(video.name)}"
                     onload="this.classList.add('loaded')" 
                     onerror="this.style.display='none'; this.nextElementSibling.style.display='flex';">`
              : `<div class="thumbnail-placeholder no-thumb-selected">
                   <div style="text-align: center;">
                     <div style="font-size: 14px; opacity: 0.8;">No Preview Available</div>
                   </div>
                 </div>`
          }
          ${hasThumbnail ? `<div class="thumbnail-placeholder" style="display: none;">🎬</div>` : ""}
          <div class="play-button">▶</div>
          <div class="video-duration">HD</div>
          <div class="format-badge">${selectedFormat.toUpperCase()}</div>
        </div>
        <div class="video-content">
          <div class="video-title">${escapeHtml(getFormattedVideoName(video.name, selectedFormat))}</div>
        </div>
      </div>
    `
    })
    .join("")

  videoGrid.classList.remove("hidden")
  updateResultsInfo(videos.length)

  document.querySelectorAll(".video-item").forEach((item) => {
    item.addEventListener("click", () => {
      const index = Number.parseInt(item.getAttribute("data-index"))
      playVideo(videos[index], item)
    })
  })
}

function escapeHtml(text) {
  const div = document.createElement("div")
  div.textContent = text
  return div.innerHTML
}

function getFormattedVideoName(name, format) {
  const baseName = name.replace(/\.[^/.]+$/, "") // Remove existing extension
  const extension = format === "dash" ? ".mpd" : ".m3u8"
  return baseName + extension
}

function playVideo(video, videoItem) {
  if (activeVideoItem) activeVideoItem.classList.remove("active")
  videoItem.classList.add("active")
  activeVideoItem = videoItem

  videoPlayerSection.classList.remove("hidden")
  currentVideoTitle.textContent = getFormattedVideoName(video.name, selectedFormat)

  currentFormat.textContent = selectedFormat.toUpperCase()

  cleanupPlayers()
  videoPlayerSection.scrollIntoView({ behavior: "smooth", block: "start" })

  const videoUrl = getVideoUrl(video, selectedFormat)

  if (selectedFormat === "hls") {
    setupHLSPlayer(videoUrl)
  } else if (selectedFormat === "dash") {
    setupDASHPlayer(videoUrl)
  }
}

function getVideoUrl(video, format) {
  if (format === "dash") {
    return video.videopath.replace(".m3u8", ".mpd").replace("/hls/", "/dash/")
  }
  return video.videopath
}

function setupHLSPlayer(videoUrl) {
  if (Hls && Hls.isSupported()) {
    currentHls = new Hls({
      lowLatencyMode: true,
      enableWorker: true,
      maxBufferLength: 30,
      maxMaxBufferLength: 600,
      maxBufferSize: 60 * 1000 * 1000,
      maxBufferHole: 0.5,
    })

    currentHls.loadSource(videoUrl)
    currentHls.attachMedia(videoPlayer)

    currentHls.on(Hls.Events.MANIFEST_PARSED, () => {
      console.log("HLS manifest parsed successfully")
      videoPlayer.play().catch((err) => {
        console.warn("Autoplay failed:", err)
        showPlayButton()
      })
    })

    currentHls.on(Hls.Events.ERROR, (event, data) => {
      console.error("HLS error:", data)
      if (data.fatal) {
        switch (data.type) {
          case Hls.ErrorTypes.NETWORK_ERROR:
            currentHls.startLoad()
            break
          case Hls.ErrorTypes.MEDIA_ERROR:
            currentHls.recoverMediaError()
            break
          default:
            currentHls.destroy()
            currentHls = null
            showVideoError("Failed to load HLS video. Please try another video.")
        }
      }
    })
  } else if (videoPlayer.canPlayType("application/vnd.apple.mpegurl")) {
    videoPlayer.src = videoUrl
    videoPlayer.addEventListener("loadedmetadata", () => {
      videoPlayer.play().catch((err) => {
        console.warn("Autoplay failed:", err)
        showPlayButton()
      })
    })
  } else {
    showVideoError("Your browser doesn't support HLS. Try Chrome, Firefox, or Safari.")
  }
}

function setupDASHPlayer(videoUrl) {
  console.log(videoUrl)

  if (!dashjs) {
    showVideoError("DASH.js is not available.")
    return
  }

  const originalErrorHandler = videoPlayer.onerror
  videoPlayer.onerror = null

  try {
    currentDash = dashjs.MediaPlayer().create()
    currentDash.initialize(videoPlayer, videoUrl, true)

    const timeout = setTimeout(() => {
      showVideoError("Video loading timed out. Please try again.")
      currentDash?.reset()
      currentDash = null
    }, 10000) // 10 seconds

    currentDash.on(dashjs.MediaPlayer.events.STREAM_INITIALIZED, () => {
      clearTimeout(timeout)
      videoPlayer.onerror = originalErrorHandler
      videoPlayer.play().catch((err) => {
        console.warn("Autoplay failed:", err)
        showPlayButton()
      })
    })

    currentDash.on(dashjs.MediaPlayer.events.ERROR, (e) => {
      console.error("DASH error:", e)
      showVideoError("Failed to load DASH video. Please try another video.")
    })
  } catch (error) {
    console.error("DASH setup error:", error)
    showVideoError("Failed to initialize DASH player.")
  }
}

function cleanupPlayers() {
  if (currentHls) {
    currentHls.destroy()
    currentHls = null
  }

  if (currentDash) {
    currentDash.reset()
    currentDash = null
  }

  videoPlayer.src = ""
}

function showPlayButton() {
  const overlay = document.createElement("div")
  overlay.className = "play-overlay"
  overlay.innerHTML = `
    <div class="play-overlay-button">
      <div class="play-icon">▶</div>
      <p>Click to play</p>
    </div>
  `
  overlay.style.cssText = `
    position: absolute; top: 0; left: 0; width: 100%; height: 100%;
    background: rgba(0,0,0,0.7); display: flex; justify-content: center; align-items: center; z-index: 10;
  `

  const button = overlay.querySelector(".play-overlay-button")
  button.style.cssText = `text-align: center; color: white; font-size: 1.2rem;`

  const icon = overlay.querySelector(".play-icon")
  icon.style.cssText = `font-size: 4rem; margin-bottom: 1rem;`

  videoPlayer.parentNode.style.position = "relative"
  videoPlayer.parentNode.appendChild(overlay)

  overlay.addEventListener("click", () => {
    videoPlayer.play()
    overlay.remove()
  })
}

function showVideoError(message) {
  currentVideoTitle.textContent = `Error: ${message}`
  videoPlayer.src = ""

  const overlay = document.createElement("div")
  overlay.innerHTML = `
    <div style="text-align: center; color: #dc2626; padding: 2rem;">
      <div style="font-size: 3rem; margin-bottom: 1rem;">⚠️</div>
      <p>${message}</p>
    </div>
  `
  overlay.style.cssText = `
    position: absolute; top: 0; left: 0; width: 100%; height: 100%;
    background: #f8fafc; display: flex; align-items: center; justify-content: center; border-radius: 12px;
  `

  videoPlayer.parentNode.style.position = "relative"
  videoPlayer.parentNode.appendChild(overlay)

  setTimeout(() => {
    if (overlay.parentNode) overlay.remove()
  }, 5000)
}

function updateResultsInfo(count) {
  resultsInfo.textContent =
    count === 1
      ? `Found 1 video (${selectedFormat.toUpperCase()} format)`
      : `Found ${count} videos (${selectedFormat.toUpperCase()} format)`
  resultsInfo.classList.remove("hidden")
}

function hideAllSections() {
  videoGrid.classList.add("hidden")
  resultsInfo.classList.add("hidden")
  noResults.classList.add("hidden")
  errorMessage.classList.add("hidden")
  loading.classList.add("hidden")
}

function showLoading() {
  hideAllSections()
  loading.classList.remove("hidden")
}

function showError(message) {
  hideAllSections()
  errorText.textContent = message
  errorMessage.classList.remove("hidden")
}

async function searchVideos(query) {
  if (!query || !query.trim()) {
    hideAllSections()
    videoPlayerSection.classList.add("hidden")
    return
  }

  showLoading()
  selectedFormat = getSelectedFormat()

  try {
    const searchParams = new URLSearchParams({ q: query.trim(), format: selectedFormat })
    const response = await fetch(`${API_BASE_URL}?${searchParams}`)

    if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`)
    const videos = await response.json()
    console.log()
    videos.forEach((video) => (video.format = selectedFormat))
    currentVideos = videos
    renderVideos(videos)
  } catch (error) {
    console.error("Search error:", error)
    if (error.name === "TypeError" && error.message.includes("fetch")) {
      showError("Unable to connect to the server. Please check if the API is running.")
    } else if (error.message.includes("404")) {
      showError("API endpoint not found.")
    } else if (error.message.includes("500")) {
      showError("Server error occurred.")
    } else {
      showError("Failed to search videos.")
    }
  }
}

const debouncedSearch = debounce(searchVideos, 500)

function debounce(func, wait) {
  let timeout
  return (...args) => {
    clearTimeout(timeout)
    timeout = setTimeout(() => func(...args), wait)
  }
}

searchInput.addEventListener("input", (e) => debouncedSearch(e.target.value))
searchButton.addEventListener("click", (e) => {
  e.preventDefault()
  const query = searchInput.value.trim()
  if (query) searchVideos(query)
})
searchInput.addEventListener("keydown", (e) => {
  if (e.key === "Enter") {
    e.preventDefault()
    const query = searchInput.value.trim()
    if (query) searchVideos(query)
  }
})

document.addEventListener("DOMContentLoaded", () => {
  setTimeout(() => searchInput.focus(), 100)
  selectedFormat = getSelectedFormat()
})

document.addEventListener("keydown", (e) => {
  if (e.key === "/" && e.target !== searchInput) {
    e.preventDefault()
    searchInput.focus()
  }
  if (e.key === "Escape" && e.target === searchInput) {
    searchInput.value = ""
    hideAllSections()
    videoPlayerSection.classList.add("hidden")
  }
})

window.addEventListener("beforeunload", () => {
  cleanupPlayers()
})
