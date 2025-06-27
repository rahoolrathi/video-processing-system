class TranscodingProfilesAdmin {
    constructor() {
      this.profiles = [];
      this.currentProfileId = null;
      this.apiBaseUrl = "https://localhost:7167/api/admin";
  
      // DOM Elements
      this.profilesList = document.getElementById("profilesList");
      this.profileEditor = document.getElementById("profileEditor");
      this.profileForm = document.getElementById("profileForm");
      this.editorTitle = document.getElementById("editorTitle");
      this.confirmModal = document.getElementById("confirmModal");
  
      this.initEventListeners();
      this.loadProfiles();
    }
  
    initEventListeners() {
      document.getElementById("addProfileBtn").addEventListener("click", () => {
        this.showProfileEditor();
      });
  
      document.getElementById("closeEditorBtn").addEventListener("click", () => {
        this.hideProfileEditor();
      });
  
      document.getElementById("cancelBtn").addEventListener("click", () => {
        this.hideProfileEditor();
      });
  
      this.profileForm.addEventListener("submit", (e) => {
        e.preventDefault();
        this.saveProfile();
      });
  
      document.querySelector(".close-modal").addEventListener("click", () => {
        this.hideConfirmModal();
      });
  
      document.getElementById("cancelDeleteBtn").addEventListener("click", () => {
        this.hideConfirmModal();
      });
  
      document.getElementById("confirmDeleteBtn").addEventListener("click", () => {
        this.deleteProfile();
        this.hideConfirmModal();
      });
    }
  
    async loadProfiles() {
      try {
        const res = await fetch(`${this.apiBaseUrl}/profiles`);
        this.profiles = await res.json();
        this.renderProfilesList();
      } catch (error) {
        console.error("Failed to load profiles:", error);
        this.profilesList.innerHTML = `<div class="profile-list-empty">Failed to load profiles. Please try again.</div>`;
      }
    }
  
    renderProfilesList() {
      if (this.profiles.length === 0) {
        this.profilesList.innerHTML = `<div class="profile-list-empty">No profiles found. Click "Add New Profile" to create one.</div>`;
        return;
      }
    
      this.profilesList.innerHTML = this.profiles.map(profile => `
        <div class="profile-item" data-id="${profile.id}">
          <div class="profile-name">${profile.profileName}</div>
          <div class="profile-details">
            <div class="profile-detail"><span class="profile-detail-label">Resolution:</span> <span>${profile.resolutions}</span></div>
            <div class="profile-detail"><span class="profile-detail-label">Bitrate:</span> <span>${profile.bitratesKbps}</span></div>
          </div>
          <div class="profile-formats">
            ${profile.formats.map(format => `<span class="format-tag">${format.formatType}</span>`).join('')}
          </div>
        </div>
      `).join('');
    }
    
  
    showProfileEditor() {
      this.profileForm.reset();
      document.getElementById("profileId").value = "";
      this.editorTitle.textContent = "Add New Profile";
      this.currentProfileId = null;
      this.profileEditor.style.display = "block";
    }
  
    hideProfileEditor() {
      this.profileEditor.style.display = "none";
    }
  
  
  
    async saveProfile() {
        const id = document.getElementById("profileId").value.trim();
        const profileName = document.getElementById("profileName").value.trim();
        const resolutions = document.getElementById("resolution").value.trim();
        const bitratesKbps = document.getElementById("bitrate").value.trim();
        const formatTypes = [];
      
        if (document.getElementById("formatHLS").checked) {
          formatTypes.push({ formatType: "HLS" });
        }
        if (document.getElementById("formatDASH").checked) {
          formatTypes.push({ formatType: "DASH" });
        }
      
        if (!profileName || !resolutions || !bitratesKbps || formatTypes.length === 0) {
          alert("Please fill in all required fields and select at least one format.");
          return;
        }
      
       
        const payload = {
          profileName,
          resolutions,
          bitratesKbps,
          formats: formatTypes
        };
      
      
       
        try {
          
            // POST (Create)
            await fetch(`${this.apiBaseUrl}/profiles`, {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify(payload)
            });
            alert("Profile created successfully!");
          
      
          await this.loadProfiles();
          this.hideProfileEditor();
        } catch (error) {
          console.error("Failed to save profile:", error);
          alert("An error occurred while saving the profile.");
        }
      }
      
  
  
  
   
  
  }
  
  document.addEventListener("DOMContentLoaded", () => {
    new TranscodingProfilesAdmin();
  });
  