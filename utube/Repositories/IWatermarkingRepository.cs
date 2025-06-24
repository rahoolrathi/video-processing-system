using Microsoft.EntityFrameworkCore;
using utube.Data;
using utube.DTOs;
using utube.Enums;
using utube.Models;

namespace utube.Repositories
{
    public interface IWatermarkingRepository
    {
        Task<Watermarking> AddAsync(WatermarkingRequestDto request);
        Task UpdateStatusAsync(Guid jobId, JobStatus newStatus);
        Task UpdatePathAndStatusAsync(Guid jobId, string? watermarkPath, JobStatus newStatus);
        Task <JobStatus>GetStatus(Guid jobId);
        Task<List<Watermarking>> GetAllCompletedJobsAsync();
    }

    public class WatermarkingRepository : IWatermarkingRepository
    {
        private readonly AppDbContext _context;

        public WatermarkingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Watermarking> AddAsync(WatermarkingRequestDto request)
        {
            var job = new Watermarking
            {
                VideoId = request.VideoId,
                text = request.text,
                Status = JobStatus.Queued
            };

            _context.WatermarkingJobs.Add(job);
            await _context.SaveChangesAsync();

            return job;
        }
        public async Task UpdateStatusAsync(Guid jobId, JobStatus newStatus)
        {
            var job = await _context.WatermarkingJobs.FindAsync(jobId);
            if (job == null)
            {
                throw new KeyNotFoundException($"Watermarking job with ID {jobId} not found.");
            }

            job.Status = newStatus;
            await _context.SaveChangesAsync();
        }
        public async Task UpdatePathAndStatusAsync(Guid jobId, string? watermarkPath, JobStatus newStatus)
        {
            var job = await _context.WatermarkingJobs.FindAsync(jobId);
            if (job == null)
            {
                throw new KeyNotFoundException($"Watermarking job with ID {jobId} not found.");
            }

            job.WatermarkPath = watermarkPath ?? string.Empty;
            job.Status = newStatus;
        

            await _context.SaveChangesAsync();
        }

        public async Task<JobStatus> GetStatus(Guid jobId)
        {
            var job = await _context.WatermarkingJobs.FindAsync(jobId);
            if (job == null)
            {
                throw new KeyNotFoundException($"Watermarking job with ID {jobId} not found.");
            }

            return job.Status;
        }
        public async Task<List<Watermarking>> GetAllCompletedJobsAsync()
        {
            return await _context.WatermarkingJobs
                .Where(w => w.Status == JobStatus.Done)
                .ToListAsync();
        }


    }
}
