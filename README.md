# Video Processing System

A scalable, modular, and cloud-ready video processing platform built with .NET 8. This system supports chunked video uploads, distributed transcoding, thumbnail generation, watermarking, and advanced search with Elasticsearch. It is designed for extensibility and can be deployed in cloud or on-premise environments.

---

## 🚀 Features

- **Chunked Video Uploads** with resume support and Azure Blob Storage integration
- **Transcoding** to HLS/DASH using FFmpeg and distributed background workers
- **Thumbnail Generation** with automatic extraction and user selection
- **Watermarking** with customizable text overlays
- **Elasticsearch-powered Search** for fast video discovery
- **Secure Signed URLs** for uploads and streaming
- **RabbitMQ** for distributed job processing
- **Extensible Profiles** for encoding and formats

---

## 🏗️ Architecture Overview

- **Frontend**: Communicates via REST API, supports chunked uploads and real-time status updates.
- **Backend**: ASP.NET Core Web API (.NET 8)
- **Database**: SQL Server (Entity Framework Core)
- **Storage**: Azure Blob Storage
- **Queue**: RabbitMQ
- **Search**: Elasticsearch
- **Media Processing**: FFmpeg

### Main Components

- `Controllers/` - API endpoints for upload, transcoding, thumbnails, watermarking, search, and admin
- `Services/` - Business logic, background consumers, Azure/RabbitMQ/Elasticsearch integration
- `Repositories/` - Data access for all main entities
- `Models/` - Entity definitions (Video, TranscodeJob, ThumbnailJob, Watermarking, EncodingProfile, Format)
- `DTOs/` - Data transfer objects for API and queue messages

---

## ⚙️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [RabbitMQ](https://www.rabbitmq.com/download.html)
- [Elasticsearch](https://www.elastic.co/downloads/elasticsearch)
- [FFmpeg](https://ffmpeg.org/download.html)
- [Azure Storage Account](https://portal.azure.com/)

---

## 🔧 Configuration

1. **Clone the repository:**
