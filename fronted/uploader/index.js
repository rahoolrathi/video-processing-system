//The file stays on disk (via File API or input element).

/*
 Downsides of Option 2 (Client → Azure → Backend Downloads)
 Wasted bandwidth: Upload to Azure, then download to merge = 2× data transfer.
 Slower: Merging is delayed by download time.
 More complex code: Needs retry, range downloading, parallel download, etc.



*/


// Create the blob service client once

class ChunkedVideoUploader {
  constructor() {
    this.file = null
    this.chunkSize = 1024 * 1024 // 1MB
    this.totalChunks = 0
    this.currentChunk = 0
    this.uploadId = null
    this.uthjobid = null
    this.uploadPaused = false
    this.uploadedChunks = new Set()
    this.selectedThumbnail = null
    this.thumbnails = []
    this.generatedurl=null;
    this.blobname=null;
    this.blockIdList = [];

    this.watermarkJobId = null;
this.watermarkStatus = null;
this.watermarkProgress = 0;


    this.publicurl=null;
    this.apiBaseUrl = "https://localhost:7167/api"

    // Will be populated from API
    this.transcodingProfiles = {}

    this.initializeEventListeners()
    this.setupConnectivityListeners()

    // Fetch profiles from API when the class is initialized
    this.fetchTranscodingProfiles()
  }

  initializeEventListeners() {
    const uploadArea = document.getElementById("uploadArea")
    const fileInput = document.getElementById("fileInput")
    const startBtn = document.getElementById("startBtn")

    // Upload area events
    uploadArea.addEventListener("dragover", (e) => {
      e.preventDefault()
      uploadArea.classList.add("dragover")
    })

    uploadArea.addEventListener("dragleave", () => {
      uploadArea.classList.remove("dragover")
    })

    uploadArea.addEventListener("drop", (e) => {
      e.preventDefault()
      uploadArea.classList.remove("dragover")
      const files = e.dataTransfer.files
      if (files.length > 0) {
        this.handleFileSelect(files[0])
      }
    })

    fileInput.addEventListener("change", (e) => {
      if (e.target.files.length > 0) {
        this.handleFileSelect(e.target.files[0])
      }
    })

    startBtn.addEventListener("click", () => this.startUpload())

    // Processing section events
    document.getElementById("thumbnailBtn").addEventListener("click", () => {
   this.showThumbnailSection();
      //this.startThumbnailGeneration()
    })
    document.getElementById("startthumbnailbtn").addEventListener("click", () => {
      this.startThumbnailGeneration()
    })

    document.getElementById("transcodingBtn").addEventListener("click", () => {
      this.showTranscodingSection()
    })

    document.getElementById("watermarkBtn").addEventListener("click", () => {
      this.showWatermarkingSection()
    })

    // Back button now resets for new upload
    document.getElementById("backBtn").addEventListener("click", () => {
      this.resetForNewUpload()
    })

    // Thumbnail section events
    document.getElementById("selectCoverBtn").addEventListener("click", () => {
      this.setVideoCover()
    })

    document.getElementById("backToProcessingBtn").addEventListener("click", () => {
      this.showProcessingSection()
    })

    // Transcoding section events
    document.getElementById("profileSelect").addEventListener("change", (e) => {
      this.handleProfileSelection(e.target.value)
    })

    document.getElementById("transcodeBtn").addEventListener("click", () => {
      this.startTranscoding()
    })

   

    document.getElementById("backToTranscodingProcessingBtn").addEventListener("click", () => {
      this.showProcessingSection()
    })

    // Watermarking section events
    document.getElementById("newWatermarkBtn").addEventListener("click", () => {
      this.showWatermarkModal()
    })

    document.getElementById("backToProcessingFromWatermarkBtn").addEventListener("click", () => {
      this.showProcessingSection()
    })

    // Watermark modal events
    document.getElementById("closeWatermarkModal").addEventListener("click", () => {
      this.hideWatermarkModal()
    })

    document.getElementById("backWatermarkBtn").addEventListener("click", () => {
      this.hideWatermarkModal()
    })

    document.getElementById("applyWatermarkBtn").addEventListener("click", () => {
      this.applyWatermark()
    })

    document.getElementById("watermarkText").addEventListener("input", (e) => {
      this.updateCharCounter(e.target.value.length)
    })

    // document.getElementById("watermarkOpacity").addEventListener("input", (e) => {
    //   this.updateOpacityDisplay(e.target.value)
    // })

    // Close modal when clicking outside
    document.getElementById("watermarkModal").addEventListener("click", (e) => {
      if (e.target.id === "watermarkModal") {
        this.hideWatermarkModal()
      }
    })
document.getElementById("playVideoBtn").addEventListener("click", () => {
        this.showVideoPlayer()
    })

    // NEW: Copy URL button event listeners
    document.getElementById("copyUrlBtn").addEventListener("click", () => {
        this.copyPublicUrl("publicUrlDisplay")
    })

    document.getElementById("modalCopyBtn").addEventListener("click", () => {
        this.copyPublicUrl("modalPublicUrl")
    })

    // NEW: Video modal event listeners
    document.getElementById("closeVideoModal").addEventListener("click", () => {
        this.closeVideoPlayer()
    })

    document.getElementById("closeVideoPlayerBtn").addEventListener("click", () => {
        this.closeVideoPlayer()
    })

    // Close modal when clicking outside
    document.getElementById("videoPlayerModal").addEventListener("click", (e) => {
        if (e.target.id === "videoPlayerModal") {
            this.closeVideoPlayer()
        }
    })




  }
 
// NEW: Show video player method
showVideoPlayer() {
  if (!this.publicurl) {
      this.showStatus("No public URL available. Please complete the upload first.", "error")
      return
  }

  // Set video source and info
  const modalPlayer = document.getElementById("modalVideoPlayer")
  const modalTitle = document.getElementById("modalVideoTitle")
  const modalUrl = document.getElementById("modalPublicUrl")

  modalPlayer.src = this.publicurl
  modalTitle.textContent = this.file ? this.file.name : "Uploaded Video"
  modalUrl.value = this.publicurl

  // Show modal
  document.getElementById("videoPlayerModal").style.display = "flex"

  // Add error handling
  modalPlayer.onerror = () => {
      this.showStatus("❌ Failed to load video. The file may be corrupted or in an unsupported format.", "error")
  }

  modalPlayer.onloadedmetadata = () => {
      console.log("✅ Video loaded successfully in modal")
  }
}


// NEW: Close video player method
closeVideoPlayer() {
  const modal = document.getElementById("videoPlayerModal")
  const modalPlayer = document.getElementById("modalVideoPlayer")
  
  // Pause video and reset
  modalPlayer.pause()
  modalPlayer.currentTime = 0
  
  // Hide modal
  modal.style.display = "none"
}
 
  // NEW: Copy public URL method
async copyPublicUrl(inputId) {
  const input = document.getElementById(inputId)
  const url = input.value

  if (!url) {
      this.showStatus("No URL available to copy.", "error")
      return
  }

  try {
      await navigator.clipboard.writeText(url)
      this.showCopyFeedback(inputId)
      this.showStatus("✅ URL copied to clipboard!", "success")
  } catch (err) {
      // Fallback for older browsers
      input.select()
      document.execCommand('copy')
      this.showCopyFeedback(inputId)
      this.showStatus("✅ URL copied to clipboard!", "success")
  }
}

// NEW: Show copy feedback method
showCopyFeedback(inputId) {
  const button = inputId === "publicUrlDisplay" ? 
      document.getElementById("copyUrlBtn") : 
      document.getElementById("modalCopyBtn")
  
  const originalText = button.textContent
  const originalBg = button.style.backgroundColor
  
  button.textContent = "✅ Copied!"
  button.style.backgroundColor = "#28a745"
  
  setTimeout(() => {
      button.textContent = originalText
      button.style.backgroundColor = originalBg
  }, 2000)
}

  handleFileSelect(file) {
    if (!file.type.startsWith("video/")) {
      this.showStatus("Please select a video file.", "error")
      return
    }

    this.file = file
    this.totalChunks = Math.ceil(file.size / this.chunkSize)
    this.displayFileInfo()
  }

  displayFileInfo() {
    const fileInfo = document.getElementById("fileInfo")
    const fileDetails = document.getElementById("fileDetails")

    fileDetails.innerHTML = `
            <p><strong>Name:</strong> ${this.file.name}</p>
            <p><strong>Size:</strong> ${this.formatFileSize(this.file.size)}</p>
            <p><strong>Type:</strong> ${this.file.type}</p>
            <p><strong>Total Chunks:</strong> ${this.totalChunks}</p>
            <p><strong>Chunk Size:</strong> ${this.formatFileSize(this.chunkSize)}</p>
        `

    fileInfo.style.display = "block"
    document.getElementById("controls").style.display = "block"
  }



async startUpload() {
  document.getElementById("progressContainer").style.display = "block";

  try {
    await this.initializeUpload();
    this.loadUploadedChunksFromStorage();
    this.setupConnectivityListeners();
    await this.uploadChunks();
  } catch (error) {
    this.showStatus(`Upload failed: ${error.message}`, "error");
  }
}

async initializeUpload() {
  const initData = {
    OriginalFilename: this.file.name,
    fileSize: this.file.size,
    TotalChunks: this.totalChunks,
    chunkSize: this.chunkSize,
  };

  const response = await fetch(`${this.apiBaseUrl}/uploader`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(initData),
  });

  if (!response.ok) throw new Error("Failed to initialize upload");

  const data = await response.json();
  this.uploadId = data.id;
  this.blobname = data?.blobPath;
  this.generatedurl = data?.uploadUrl;
  this.blockIdList = [];

  console.log("Upload initialized successfully! ID: " + this.uploadId);
  this.showStatus("Upload initialized successfully! ID: " + this.uploadId, "success");

  localStorage.setItem(`upload_${this.uploadId}_info`, JSON.stringify({ uploadedChunks: [], blobname: this.blobname, url: this.generatedurl }));
}

loadUploadedChunksFromStorage() {
  const stored = localStorage.getItem(`upload_${this.uploadId}_info`);
  if (stored) {
    const data = JSON.parse(stored);
    this.uploadedChunks = new Set(data.uploadedChunks || []);
    this.blobname = data.blobname;
    this.generatedurl = data.url;
    this.blockIdList = [...this.uploadedChunks].map(i => btoa(String(i).padStart(6, '0')));
    this.currentChunk = Math.max(...this.uploadedChunks) + 1;
    this.updateProgress();
  } else {
    this.uploadedChunks = new Set();
    this.currentChunk = 0;
  }
}

async uploadChunks() {
  for (let i = 0; i < this.totalChunks; i++) {
    if (this.uploadPaused) break;
    if (this.uploadedChunks.has(i)) continue;

    try {
      await this.uploadChunk(i);
      this.uploadedChunks.add(i);
      this.currentChunk = i + 1;
      this.updateProgress();

      localStorage.setItem(
        `upload_${this.uploadId}_info`,
        JSON.stringify({
          uploadedChunks: [...this.uploadedChunks],
          blobname: this.blobname,
          url: this.generatedurl,
        })
      );

      await new Promise((resolve) => setTimeout(resolve, 10));
    } catch (err) {
      this.showStatus(`Error uploading chunk ${i}: ${err.message}`, "error");
      throw err;
    }
  }

  if (!this.uploadPaused) {
    this.showStatus("✅ All chunks uploaded successfully!", "success");
    setTimeout(() => {
      this.showProcessingSection();
    }, 2000);
  }
}

async uploadChunk(chunkIndex) {
  const start = chunkIndex * this.chunkSize;
  const end = Math.min(start + this.chunkSize, this.file.size);
  const chunk = this.file.slice(start, end);
  const isLastChunk = end >= this.file.size;

  const blockId = btoa(String(chunkIndex).padStart(6, '0'));
  const pipeline = azblob.StorageURL.newPipeline(new azblob.AnonymousCredential());
  const blobURL = new azblob.BlockBlobURL(this.generatedurl, pipeline);

  await blobURL.stageBlock(
    azblob.Aborter.none,
    blockId,
    chunk,
    chunk.size
  );

  this.blockIdList.push(blockId);
  console.log(`✅ Staged chunk ${chunkIndex}`);

  if (isLastChunk) {
    await blobURL.commitBlockList(
      azblob.Aborter.none,
      this.blockIdList,
      {
        blobHTTPHeaders: { blobContentType: this.file.type },
      }
    );
    console.log("🎉 Azure: All chunks committed successfully.");
    localStorage.removeItem(`upload_${this.uploadId}_info`);


    ///////////////////////////////


     // 🔔 Notify backend that video is fully uploaded
  try {
    const notifyResponse = await fetch(`${this.apiBaseUrl}/uploader/complete/${this.uploadId}`, {
      method: "POST",
    });

    if (!notifyResponse.ok) {
      throw new Error(`Backend failed to mark upload complete.`);
    }

    const result = await notifyResponse.json(); 
    this.publicurl=result.publicUrl;
    console.log("✅ Backend marked upload as complete.", result);
    console.log("✅ Backend marked upload as complete.",  this.publicurl);
  } catch (err) {
    console.error("⚠️ Failed to notify backend of upload completion:", err);
    this.showStatus("⚠️ Upload complete but backend notification failed.", "error");
  }
  
  }
}

pauseUpload() {
  this.uploadPaused = true;
  this.showStatus("⏸ Upload paused.", "info");
}

resumeUpload() {
  this.uploadPaused = false;
  this.uploadChunks();
  this.showStatus("▶️ Resumed upload.", "info");
}

setupConnectivityListeners() {
  window.addEventListener("offline", () => {
    this.pauseUpload();
    this.showStatus("🔴 Offline. Upload paused.", "error");
  });

  window.addEventListener("online", () => {
    if (this.uploadPaused) {
      this.showStatus("🟢 Back online. Resuming upload...", "info");
      this.resumeUpload();
    }
  });
}

updateProgress() {
  const percentage = Math.round((this.uploadedChunks.size / this.totalChunks) * 100);
  document.getElementById("progressFill").style.width = percentage + "%";
  document.getElementById("progressText").textContent = percentage + "%";
  document.getElementById("chunkInfo").textContent = `Uploaded ${this.uploadedChunks.size} of ${this.totalChunks} chunks`;
}

showStatus(message, type) {
  const status = document.getElementById("status");
  status.textContent = message;
  status.className = `status ${type}`;
  status.style.display = "block";

  if (type === "success" || type === "error") {
    setTimeout(() => {
      status.style.display = "none";
    }, 5000);
  }
}



  formatFileSize(bytes) {
    if (bytes === 0) return "0 Bytes"
    const k = 1024
    const sizes = ["Bytes", "KB", "MB", "GB"]
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Number.parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i]
  }

  // Thumbnail generation methods
  async startThumbnailGeneration() {
   

    const progressContainer = document.getElementById("thumbnailProgress")
    const progressFill = document.getElementById("thumbnailProgressFill")
    const progressText = document.getElementById("thumbnailProgressText")
    const statusText = document.getElementById("thumbnailStatus")

    progressContainer.style.display = "block"
    statusText.textContent = "Starting thumbnail generation..."
    const btn = document.getElementById("startthumbnailbtn"); // <-- Add this
    btn.disabled = true;             // Disable button immediately
    btn.innerText = "Generating..."; // Optional: show feedback
    btn.classList.add("generating");

    try {
      // Start thumbnail generation
      const response = await fetch(`https://localhost:7167/api/Thumbnail/start`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          videoId: this.uploadId,
        }),
      })

      if (!response.ok) {
        throw new Error(`Thumbnail generation request failed: ${response.status}`)
      }

      const result = await response.json()
      this.uthjobid = result.jobId
      console.log("Thumbnail generation started:", result)

      // Poll for status
      const pollInterval = 2000
      const poller = setInterval(async () => { 
        try {
          const statusUrl = `${this.apiBaseUrl}/Thumbnail/status/${this.uthjobid}`
          const statusResponse = await fetch(statusUrl)

          if (!statusResponse.ok) {
            clearInterval(poller)
            statusText.textContent = "❌ Failed to fetch thumbnail status."
            statusText.style.color = "#dc3545"
            btn.disabled = false;
            btn.innerText = "Start Thumbnails"; // Re-enable button
            btn.classList.remove("generating");
            return
          }

          const statusData = await statusResponse.json()
          const { status, progress, thumbnails } = statusData
          console.log(statusData)

          statusText.textContent = `Status: ${status}`

          switch (status) {
            case "Queued":
              progressFill.style.width = "5%"
              progressText.textContent = "Queued..."
              break

            case "Processing":
              const progressPercent = progress || 50
              progressFill.style.width = `${progressPercent}%`
              progressText.textContent = `${progressPercent}%`
              break

            case "Done":
              clearInterval(poller)
              progressFill.style.width = "100%"
              progressText.textContent = "100%"
              statusText.textContent = "✅ Thumbnails generated successfully!"
              statusText.style.color = "#28a745"
               
            btn.disabled = false;
            btn.innerText = "Start Thumbnails";
            btn.classList.remove("generating");

              const countRes = await fetch(`${this.apiBaseUrl}/Thumbnail/count/${this.uthjobid}`)
              const countData = await countRes.json()
              const totalThumbnails = countData.count
              const baseUrl = "https://rahool.blob.core.windows.net/utube"
              const folderPath = `thumbnails/${this.uploadId}`
              this.thumbnails = Array.from({ length: totalThumbnails }, (_, i) => ({
                url: `${baseUrl}/${folderPath}/thumb_${String(i + 1).padStart(3, "0")}.jpg`,
              }))
              // Hide progress and show thumbnails
              setTimeout(() => {
                progressContainer.style.display = "none"
                this.displayThumbnails()
              }, 1000)
              break

            case "Error":
              clearInterval(poller)
              progressFill.style.width = "100%"
              progressText.textContent = "Failed"
              statusText.textContent = `❌ Thumbnail generation failed`
              statusText.style.color = "#dc3545"
              break
          }
        } catch (error) {
          clearInterval(poller)
          statusText.textContent = `❌ Error checking status: ${error.message}`
          statusText.style.color = "#dc3545"
        }
      }, pollInterval)
    } catch (error) {
      statusText.textContent = `❌ Failed to start thumbnail generation: ${error.message}`
      statusText.style.color = "#dc3545"
    }
  }

  displayThumbnails() {
    const grid = document.getElementById("thumbnailGrid");
  
    // Clear and populate grid with thumbnails
    grid.innerHTML = this.thumbnails
      .map(
        (thumb, index) => `
          <div class="thumbnail-item" data-index="${index}">
            <img src="${thumb.url}" alt="Thumbnail ${index + 1}" />
            <div class="thumbnail-overlay">
              <button class="thumbnail-select-btn">Select</button>
            </div>
          </div>
        `
      )
      .join("");
  
    // Add click handlers for thumbnail selection
    grid.querySelectorAll(".thumbnail-item").forEach((item) => {
      item.addEventListener("click", () => {
        this.selectThumbnail(Number.parseInt(item.dataset.index));
      });
    });
  
    grid.style.display = "grid";
  }
  

  selectThumbnail(index) {
    // Remove previous selection
    document.querySelectorAll(".thumbnail-item").forEach((item) => {
      item.classList.remove("selected")
    })

    // Add selection to clicked thumbnail
    const selectedItem = document.querySelector(`[data-index="${index}"]`)
    selectedItem.classList.add("selected")

    this.selectedThumbnail = this.thumbnails[index]
    console.log(this.selectedThumbnail)

    // Enable the set cover button
    document.getElementById("selectCoverBtn").disabled = false
  }

  async setVideoCover() {
    if (!this.selectedThumbnail) {
      this.showStatus("Please select a thumbnail first.", "error")
      return
    }

    try {
      const response = await fetch(`${this.apiBaseUrl}/Thumbnail/select`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          JobId: this.uthjobid,
          selectedImageName: this.selectedThumbnail.url,
        }),
      })

      if (!response.ok) {
        throw new Error(`Failed to set video cover: ${response.status}`)
      }

      this.showStatus("✅ Video cover set successfully!", "success")
      this.selectedThumbnail = null
      document.getElementById("selectCoverBtn").disabled = true
      document.getElementById("thumbnailGrid").style.display = "none"
      // Go back to processing section (not reset)
      setTimeout(() => {
        this.showProcessingSection()
      }, 2000)
    } catch (error) {
      this.showStatus(`❌ Failed to set video cover: ${error.message}`, "error")
    }
  }

  showThumbnailSection() {
    document.getElementById("processingSection").style.display = "none"
    document.getElementById("uploadSection").style.display = "none"
    document.getElementById("transcodingSection").style.display = "none"
    document.getElementById("watermarkingSection").style.display = "none"
    document.getElementById("thumbnailSection").style.display = "block"

    // Reset thumbnail state
    this.selectedThumbnail = null
    document.getElementById("selectCoverBtn").disabled = true
    //document.getElementById("thumbnailGrid").style.display = "none"
  }

  // New methods for video processing
  showProcessingSection() {
    document.getElementById("uploadSection").style.display = "none"
    document.getElementById("transcodingSection").style.display = "none"
    document.getElementById("thumbnailSection").style.display = "none"
    document.getElementById("watermarkingSection").style.display = "none"
    document.getElementById("processingSection").style.display = "block"

    // Display uploaded video info
    const videoDetails = document.getElementById("uploadedVideoDetails")
    if (this.file && this.uploadId) {
      videoDetails.innerHTML = `
              <p><strong>File:</strong> ${this.file.name}</p>
              <p><strong>Size:</strong> ${this.formatFileSize(this.file.size)}</p>
              <p><strong>Upload ID:</strong> ${this.uploadId}</p>
              <p><strong>Status:</strong> <span style="color: #28a745;">✅ Upload Complete</span></p>
          `
    }
     // Handle public URL display and play button
    const playVideoBtn = document.getElementById("playVideoBtn")
    const publicUrlInfo = document.getElementById("publicUrlInfo")
    const publicUrlDisplay = document.getElementById("publicUrlDisplay")

    if (this.publicurl) {
        // Show public URL section
        publicUrlInfo.style.display = "block"
        publicUrlDisplay.value = this.publicurl
        
        // Enable play button
        playVideoBtn.disabled = false
        playVideoBtn.title = "Click to play your uploaded video"
    } else {
        // Hide public URL section
        publicUrlInfo.style.display = "none"
        
        // Disable play button
        playVideoBtn.disabled = true
        playVideoBtn.title = "Public URL not available yet"
    }

  }

  showUploadSection() {
    document.getElementById("processingSection").style.display = "none"
    document.getElementById("transcodingSection").style.display = "none"
    document.getElementById("thumbnailSection").style.display = "none"
    document.getElementById("watermarkingSection").style.display = "none"
    document.getElementById("uploadSection").style.display = "block"
  }

  showTranscodingSection() {
    document.getElementById("processingSection").style.display = "none"
    document.getElementById("uploadSection").style.display = "none"
    document.getElementById("thumbnailSection").style.display = "none"
    document.getElementById("watermarkingSection").style.display = "none"
    document.getElementById("transcodingSection").style.display = "block"
  }

  // Watermarking methods
  showWatermarkingSection() {
    document.getElementById("processingSection").style.display = "none"
    document.getElementById("uploadSection").style.display = "none"
    document.getElementById("transcodingSection").style.display = "none"
    document.getElementById("thumbnailSection").style.display = "none"
    document.getElementById("watermarkingSection").style.display = "block"

    this.loadExistingWatermarkedVideos()
  }

// Replace the loadExistingWatermarkedVideos method with this improved version

async loadExistingWatermarkedVideos() {
  const grid = document.getElementById("watermarkedVideosGrid");

  try {
    const response = await fetch(`${this.apiBaseUrl}/Watermarking/list`);

    if (!response.ok) {
      grid.innerHTML = '<p class="no-videos">No watermarked videos found.</p>';
      return;
    }

    const watermarkedVideos = await response.json();

    if (!watermarkedVideos.length) {
      grid.innerHTML = '<p class="no-videos">No watermarked videos found.</p>';
      return;
    }

    console.log(watermarkedVideos);

    grid.innerHTML = watermarkedVideos
      .map((video, index) => `
        <div class="video-url-item">
          <div class="watermark-info">
            <p><strong>Watermark:</strong> "${video.text}"</p>
          </div>
          <div class="url-container">
            <div class="url-box" id="urlBox-${index}">
              <a href="${video.signedWatermarkUrl}" target="_blank" class="video-url-link">
                ${video.signedWatermarkUrl}
              </a>
              <button class="copy-btn" onclick="copyToClipboard('${video.signedWatermarkUrl}', ${index})" title="Copy URL">
                📋
              </button>
            </div>
            <div class="copy-feedback" id="copyFeedback-${index}">Copied!</div>
          </div>
        </div>
      `)
      .join("");

    // Add the copy functionality
    this.addCopyFunctionality();

  } catch (error) {
    console.error("Error loading watermarked videos:", error);
    grid.innerHTML = '<p class="error-message">Failed to load watermarked videos.</p>';
  }
}

// Add this new method to handle copy functionality
addCopyFunctionality() {
  // Add copy function to global scope if it doesn't exist
  if (!window.copyToClipboard) {
    window.copyToClipboard = async (url, index) => {
      try {
        await navigator.clipboard.writeText(url);
        
        // Show feedback
        const feedback = document.getElementById(`copyFeedback-${index}`);
        feedback.style.display = 'block';
        
        // Hide feedback after 2 seconds
        setTimeout(() => {
          feedback.style.display = 'none';
        }, 2000);
        
      } catch (err) {
        // Fallback for older browsers
        const textArea = document.createElement('textarea');
        textArea.value = url;
        document.body.appendChild(textArea);
        textArea.select();
        document.execCommand('copy');
        document.body.removeChild(textArea);
        
        // Show feedback
        const feedback = document.getElementById(`copyFeedback-${index}`);
        feedback.style.display = 'block';
        setTimeout(() => {
          feedback.style.display = 'none';
        }, 2000);
      }
    };
  }
}
  
  

  // showWatermarkModal() {
  //   if (!this.file || !this.uploadId) {
  //     this.showStatus("No video file selected for watermarking.", "error")
  //     return
  //   }

  //   // Populate modal with video information
  //   document.getElementById("modalVideoName").textContent = this.file.name
  //   document.getElementById("modalVideoId").textContent = this.uploadId

  //   // Reset form
  //   document.getElementById("watermarkText").value = ""
  //   // document.getElementById("watermarkPosition").value = "top-right"
  //   // document.getElementById("watermarkOpacity").value = "0.7"
  //   // this.updateCharCounter(0)
  //   // this.updateOpacityDisplay(0.7)

  //   // Hide progress
  //   document.getElementById("watermarkProgress").style.display = "none"

  //   // Show modal
  //   document.getElementById("watermarkModal").style.display = "block"
  // }

  showWatermarkModal() {
    if (!this.file || !this.uploadId) {
      this.showStatus("No video file selected for watermarking.", "error");
      return;
    }
  
    // Show modal
    document.getElementById("watermarkModal").style.display = "block";
  
    // Populate modal info
    document.getElementById("modalVideoName").textContent = this.file.name;
    document.getElementById("modalVideoId").textContent = this.uploadId;
  
    // Reset form
    document.getElementById("watermarkText").value = "";
    this.updateCharCounter(0);
  
    // Check for active watermark job
    const inProgress = this.watermarkJobId && this.watermarkStatus !== "Done" && this.watermarkStatus !== "Error";
    
    if (inProgress) {
      this.resumeWatermarkProgressUI();
      this.disableWatermarkButtons();
    } else {
      document.getElementById("watermarkProgress").style.display = "none";
      this.enableWatermarkButtons();
    }
  }

  resumeWatermarkProgressUI() {
    const progressContainer = document.getElementById("watermarkProgress");
    const progressFill = document.getElementById("watermarkProgressFill");
    const progressText = document.getElementById("watermarkProgressText");
    const statusText = document.getElementById("watermarkStatus");
  
    progressContainer.style.display = "block";
  
    switch (this.watermarkStatus) {
      case "Queued":
        progressFill.style.width = "10%";
        progressText.textContent = "Queued...";
        statusText.textContent = "Status: Queued";
        break;
  
      case "Processing":
        const percent = this.watermarkProgress || 50;
        progressFill.style.width = `${percent}%`;
        progressText.textContent = `${percent}%`;
        statusText.textContent = "Status: Processing";
        break;
    }
  }
  
  disableWatermarkButtons() {
    document.getElementById("applyWatermarkBtn").disabled = true;
    document.getElementById("watermarkText").disabled = true;
  }
  
  enableWatermarkButtons() {
    document.getElementById("applyWatermarkBtn").disabled = false;
    document.getElementById("watermarkText").disabled = false;
  }
  

  hideWatermarkModal() {
    document.getElementById("watermarkModal").style.display = "none"
  }

  updateCharCounter(length) {
    document.querySelector(".char-counter").textContent = `${length}/100 characters`
  }

 

  // async applyWatermark() {
  //   const watermarkText = document.getElementById("watermarkText").value.trim()
  //   // const position = document.getElementById("watermarkPosition").value
  //   // const opacity = Number.parseFloat(document.getElementById("watermarkOpacity").value)

  //   if (!watermarkText) {
  //     this.showStatus("Please enter watermark text.", "error")
  //     return
  //   }

  //   const progressContainer = document.getElementById("watermarkProgress")
  //   const progressFill = document.getElementById("watermarkProgressFill")
  //   const progressText = document.getElementById("watermarkProgressText")
  //   const statusText = document.getElementById("watermarkStatus")

  //   progressContainer.style.display = "block"
  //   statusText.textContent = "Initializing watermark process..."

  //   // Disable apply button
  //   document.getElementById("applyWatermarkBtn").disabled = true

  //   try {
  //     // Start watermarking
  //     const response = await fetch(`${this.apiBaseUrl}/Watermarking/start`, {
  //       method: "POST",
  //       headers: { "Content-Type": "application/json" },
  //       body: JSON.stringify({
  //         videoId: this.uploadId,
  //         text: watermarkText,
  //         // position: position,
  //         // opacity: opacity,
  //       }),
  //     })

  //     if (!response.ok) {
  //       throw new Error(`Watermarking request failed: ${response.status}`)
  //     }

  //     const result = await response.json()
  //     const jobId = result.jobId

  //     // Poll for status
  //     const pollInterval = 2000
  //     const poller = setInterval(async () => {
  //       try {
  //         const statusUrl = `${this.apiBaseUrl}/Watermarking/status/${jobId}`
  //         const statusResponse = await fetch(statusUrl)

  //         if (!statusResponse.ok) {
  //           clearInterval(poller)
  //           statusText.textContent = "❌ Failed to fetch watermarking status."
  //           statusText.style.color = "#dc3545"
  //           return
  //         }

  //         const statusData = await statusResponse.json()
  //         const { status, progress, error } = statusData

  //         statusText.textContent = `Status: ${status}`

  //         switch (status) {
  //           case "Queued":
  //             progressFill.style.width = "10%"
  //             progressText.textContent = "Queued..."
  //             break

  //           case "Processing":
  //             const progressPercent = progress || 50
  //             progressFill.style.width = `${progressPercent}%`
  //             progressText.textContent = `${progressPercent}%`
  //             break

  //           case "Done":
  //             clearInterval(poller)
  //             progressFill.style.width = "100%"
  //             progressText.textContent = "100%"
  //             statusText.textContent = "✅ Watermark applied successfully!"
  //             statusText.style.color = "#28a745"

  //             setTimeout(() => {
  //               this.hideWatermarkModal()
  //               this.showStatus("Watermark applied successfully!", "success")
  //               this.loadExistingWatermarkedVideos()
  //             }, 2000)
  //             break

  //           case "Error":
  //             clearInterval(poller)
  //             progressFill.style.width = "100%"
  //             progressText.textContent = "Failed"
  //             statusText.textContent = `❌ Watermarking failed: ${error || "Unknown error"}`
  //             statusText.style.color = "#dc3545"
  //             break
  //         }
  //       } catch (error) {
  //         clearInterval(poller)
  //         statusText.textContent = `❌ Error checking status: ${error.message}`
  //         statusText.style.color = "#dc3545"
  //       }
  //     }, pollInterval)
  //   } catch (error) {
  //     statusText.textContent = `❌ Failed to apply watermark: ${error.message}`
  //     statusText.style.color = "#dc3545"
  //   } finally {
  //     document.getElementById("applyWatermarkBtn").disabled = false
  //   }
  // }
  async applyWatermark() {
    const watermarkText = document.getElementById("watermarkText").value.trim();
    if (!watermarkText) {
      this.showStatus("Please enter watermark text.", "error");
      return;
    }
  
    const progressContainer = document.getElementById("watermarkProgress");
    const progressFill = document.getElementById("watermarkProgressFill");
    const progressText = document.getElementById("watermarkProgressText");
    const statusText = document.getElementById("watermarkStatus");
  
    progressContainer.style.display = "block";
    statusText.textContent = "Initializing watermark process...";
  
    this.disableWatermarkButtons();
  
    try {
      const response = await fetch(`${this.apiBaseUrl}/Watermarking/start`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ videoId: this.uploadId, text: watermarkText }),
      });
  
      if (!response.ok) throw new Error(`Request failed: ${response.status}`);
  
      const result = await response.json();
      const jobId = result.jobId;
  
      this.watermarkJobId = jobId;
      this.watermarkStatus = "Queued";
  
      const pollInterval = 2000;
      const poller = setInterval(async () => {
        try {
          const statusUrl = `${this.apiBaseUrl}/Watermarking/status/${jobId}`;
          const statusResponse = await fetch(statusUrl);
          if (!statusResponse.ok) throw new Error("Status fetch failed");
  
          const { status, progress, error } = await statusResponse.json();
  
          this.watermarkStatus = status;
          this.watermarkProgress = progress;
  
          statusText.textContent = `Status: ${status}`;
          switch (status) {
            case "Queued":
              progressFill.style.width = "10%";
              progressText.textContent = "Queued...";
              break;
  
            case "Processing":
              const pct = progress || 50;
              progressFill.style.width = `${pct}%`;
              progressText.textContent = `${pct}%`;
              this.watermarkProgress = pct;
              break;
  
            case "Done":
              clearInterval(poller);
              this.watermarkJobId = null;
              this.watermarkStatus = "Done";
  
              progressFill.style.width = "100%";
              progressText.textContent = "100%";
              statusText.textContent = "✅ Watermark applied!";
              statusText.style.color = "#28a745";
  
              this.enableWatermarkButtons();
  
              setTimeout(() => {
                this.hideWatermarkModal();
                this.showStatus("Watermark applied!", "success");
                this.loadExistingWatermarkedVideos();
              }, 2000);
              break;
  
            case "Error":
              clearInterval(poller);
              this.watermarkJobId = null;
              this.watermarkStatus = "Error";
  
              progressFill.style.width = "100%";
              progressText.textContent = "Failed";
              statusText.textContent = `❌ ${error || "Error applying watermark"}`;
              statusText.style.color = "#dc3545";
  
              this.enableWatermarkButtons();
              break;
          }
        } catch (err) {
          clearInterval(poller);
          statusText.textContent = `❌ ${err.message}`;
          statusText.style.color = "#dc3545";
          this.enableWatermarkButtons();
        }
      }, pollInterval);
    } catch (err) {
      statusText.textContent = `❌ ${err.message}`;
      statusText.style.color = "#dc3545";
      this.enableWatermarkButtons();
    }
  }
  
  // New method to fetch transcoding profiles from API
  async fetchTranscodingProfiles() {
    try {
      const response = await fetch(`${this.apiBaseUrl}/admin/profiles`)

      if (!response.ok) {
        throw new Error(`Failed to fetch profiles: ${response.status}`)
      }

      const profiles = await response.json()

      // Process profiles and update the dropdown
      this.processProfiles(profiles)
    } catch (error) {
      console.error("Error fetching transcoding profiles:", error)
      this.showStatus("Failed to load transcoding profiles from server.", "error")
    }
  }

  // Process profiles from API and update the dropdown
  processProfiles(profiles) {
    this.transcodingProfiles = {}

    if (!profiles.length) {
      this.showStatus("No transcoding profiles available.", "error")
      return
    }

    profiles.forEach((profile) => {
      const formatTypes = profile.formats.map((format) => format.formatType)
      const formatString = formatTypes.join(", ")

      this.transcodingProfiles[profile.id] = {
        profileName: profile.profileName,
        resolution: profile.resolutions,
        bitrate: `${profile.bitratesKbps} kbps`,
        formats: formatTypes,
        formatString: formatString,
      }
    })

    this.updateProfileDropdown()
  }

  // Update the profile dropdown with available profiles
  updateProfileDropdown() {
    const profileSelect = document.getElementById("profileSelect")

    // Clear existing options except the first one
    while (profileSelect.options.length > 1) {
      profileSelect.remove(1)
    }

    // Add new options from the profiles
    Object.keys(this.transcodingProfiles).forEach((profileId) => {
      const profile = this.transcodingProfiles[profileId]
      const option = document.createElement("option")
      option.value = profileId
      option.textContent = `${profile.profileName} (${profile.formatString})`
      profileSelect.appendChild(option)
    })
  }

  handleProfileSelection(profileId) {
    const profileDetails = document.getElementById("profileDetails")
    const transcodeBtn = document.getElementById("transcodeBtn")

    if (!profileId) {
      profileDetails.style.display = "none"
      transcodeBtn.disabled = true
      return
    }

    const profile = this.transcodingProfiles[profileId]
    if (!profile) return

    // Update profile details
    document.getElementById("resolution").textContent = profile.resolution
    document.getElementById("bitrate").textContent = profile.bitrate

    // Update format display with format badges
    const formatElement = document.getElementById("format")
    formatElement.innerHTML = this.createFormatBadges(profile.formats)

    profileDetails.style.display = "block"
    transcodeBtn.disabled = false
  }

  // Create format badges for HLS and/or DASH
  createFormatBadges(formats) {
    return formats
      .map((format) => {
        const color = format === "HLS" ? "#4caf50" : "#2196f3"
        return `<span style="display: inline-block; background-color: ${color}; color: white; padding: 3px 8px; border-radius: 12px; margin-right: 5px; font-size: 12px;">${format}</span>`
      })
      .join(" ")
  }

  async startTranscoding() {
    const profileSelect = document.getElementById("profileSelect")
    const selectedProfileId = profileSelect.value

    if (!selectedProfileId) {
      this.showStatus("Please select a transcoding profile.", "error")
      return
    }

    const profile = this.transcodingProfiles[selectedProfileId]
    const statusText = document.getElementById("transcodingStatus")
    const progressContainer = document.getElementById("transcodingProgress")
    const progressFill = document.getElementById("transcodingProgressFill")
    const progressText = document.getElementById("transcodingProgressText")

    progressContainer.style.display = "block"
    statusText.textContent = "Initializing transcoding..."
    const btn = document.getElementById("transcodeBtn"); 
    btn.disabled = true;           
    btn.innerText = "Transcoding..."; 
    btn.classList.add("generating");

    try {
      // Start transcoding
      const response = await fetch(`${this.apiBaseUrl}/Transcoder/start`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          videoId: this.uploadId,
          EncodingProfileId: selectedProfileId,
        }),
      })

      if (!response.ok) {
            
        btn.disabled = false;
        btn.innerText = "Start Transcoding";
        btn.classList.remove("generating");
        throw new Error(`Transcoding request failed: ${response.status}`)
      }

      const result = await response.json()
      console.log("Transcoding started:", result)

      // Poll for status using videoId and profileId
      const pollInterval = 3000
      const poller = setInterval(async () => {
        const statusUrl = `${this.apiBaseUrl}/Transcoder/status?videoId=${this.uploadId}&profileId=${selectedProfileId}`
        const statusResponse = await fetch(statusUrl)

        if (!statusResponse.ok) {
          clearInterval(poller)
          
          statusText.textContent = "❌ Failed to fetch transcoding status."
          statusText.style.color = "#dc3545"
          btn.disabled = false;
          btn.innerText = "Start Transcoding";
          btn.classList.remove("generating");
          throw new Error(`Transcoding request failed: ${response.status}`)
          return
        }

        const statusData = await statusResponse.json()
        const { status, workerId, error } = statusData

        statusText.textContent = `Status: ${status}${workerId ? ` (Worker: ${workerId})` : ""}`

        switch (status) {
          case "Queued":
            progressFill.style.width = "5%"
            progressText.textContent = "Queued..."
            break

          case "Processing":
            const current = Number.parseInt(progressFill.style.width) || 10
            const next = Math.min(current + 10, 90)
            progressFill.style.width = `${next}%`
            progressText.textContent = `${next}%`
            break

          case "Done":
            clearInterval(poller)
            progressFill.style.width = "100%"
            progressText.textContent = "100%"
            statusText.textContent = "✅ Transcoding completed successfully!"
            statusText.style.color = "#28a745"
            btn.disabled = false;
            btn.innerText = "Start Transcoding";
            btn.classList.remove("generating");
     

            // Go back to processing section (menu page) after 3 seconds
            setTimeout(() => {
              this.showProcessingSection()
            }, 3000)
            break

          case "Error":
            clearInterval(poller)
            progressFill.style.width = "100%"
            progressText.textContent = "Failed"
            statusText.textContent = `❌ Transcoding failed: ${error || "Unknown error"}`
            statusText.style.color = "#dc3545"
            break
        }
      }, pollInterval)
    } catch (error) {
      statusText.textContent = `❌ Transcoding failed to start: ${error.message}`
      statusText.style.color = "#dc3545"
    }
  }

  resetForNewUpload() {
    // Reset all video-related data
    this.file = null
    this.uploadId = null
    this.uthjobid = null
    this.totalChunks = 0
    this.currentChunk = 0
    this.uploadedChunks = new Set()
    this.selectedThumbnail = null
    this.thumbnails = []
    this.uploadPaused = false
    this.publicurl = null
    const publicUrlDisplay = document.getElementById("publicUrlDisplay")
    const modalPublicUrl = document.getElementById("modalPublicUrl")
    if (publicUrlDisplay) publicUrlDisplay.value = ""
    if (modalPublicUrl) modalPublicUrl.value = ""
    this.closeVideoPlayer()
    // Reset UI elements
    document.getElementById("fileInfo").style.display = "none"
    document.getElementById("controls").style.display = "none"
    document.getElementById("progressContainer").style.display = "none"
    document.getElementById("status").style.display = "none"

    // Reset progress bars
    document.getElementById("progressFill").style.width = "0%"
    document.getElementById("progressText").textContent = "0%"
    document.getElementById("chunkInfo").textContent = ""

    // Reset file input
    document.getElementById("fileInput").value = ""

    // Reset transcoding section
    document.getElementById("profileSelect").selectedIndex = 0
    document.getElementById("profileDetails").style.display = "none"
    document.getElementById("transcodeBtn").disabled = true
    document.getElementById("transcodingProgress").style.display = "none"
    document.getElementById("transcodingProgressFill").style.width = "0%"
    document.getElementById("transcodingProgressText").textContent = "0%"

    // Reset thumbnail section
    document.getElementById("thumbnailProgress").style.display = "none"
    document.getElementById("thumbnailProgressFill").style.width = "0%"
    document.getElementById("thumbnailProgressText").textContent = "0%"
    document.getElementById("thumbnailGrid").innerHTML = ""
    document.getElementById("selectCoverBtn").disabled = true

    // Show upload section with success message
    this.showUploadSection()
    this.showStatus("🎉 Ready for new video upload!", "success")
  }
}

document.addEventListener("DOMContentLoaded", () => {
  new ChunkedVideoUploader()
})
