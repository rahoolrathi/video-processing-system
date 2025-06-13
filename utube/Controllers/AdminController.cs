using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using utube.DTOs;
using utube.Models;
using utube.Repositories;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IEncodingProfileRepository _profileRepository;

        public AdminController(IEncodingProfileRepository profileRepository)
        {
            _profileRepository = profileRepository;
        }

        // GET: api/admin/profiles/{id}
        [HttpGet("profiles/{id}")]
        public async Task<ActionResult<EncodingProfileWithFormatsDto>> GetProfile(Guid id)
        {
            var profile = await _profileRepository.GetByIdAsync(id);
            if (profile == null) return NotFound();

            return Ok(MapToDto(profile));
        }

        // POST: api/admin/profiles
        [HttpPost("profiles")]
        public async Task<ActionResult<EncodingProfileWithFormatsDto>> CreateProfile([FromBody] EncodingProfileWithFormatsDto dto)
        {
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState errors:");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return BadRequest(ModelState);
            }
            Console.WriteLine("hitiing");
            var profile = new EncodingProfile
            {
                Id = Guid.NewGuid(),
                ProfileName = dto.ProfileName,
                Resolutions = dto.Resolutions,
                BitratesKbps = dto.BitratesKbps,
                Formats = dto.Formats.Select(f => new Format
                {
                    Id = Guid.NewGuid(),
                    FormatType = f.FormatType
                }).ToList()
            };
            Console.WriteLine(profile);
            await _profileRepository.AddAsync(profile);
            return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, MapToDto(profile));
        }

        [HttpPut("profiles/{id}")]
        public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] EncodingProfileWithFormatsDto dto)
        {
            var existing = await _profileRepository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.ProfileName = dto.ProfileName;
            existing.Resolutions = dto.Resolutions;
            existing.BitratesKbps = dto.BitratesKbps;

            existing.Formats = dto.Formats.Select(f => new Format
            {
                FormatType = f.FormatType
            }).ToList();

            await _profileRepository.UpdateAsync(existing);
            return NoContent();
        }


        // DELETE: api/admin/profiles/{id}
        [HttpDelete("profiles/{id}")]
        public async Task<IActionResult> DeleteProfile(Guid id)
        {
            var profile = await _profileRepository.GetByIdAsync(id);
            if (profile == null) return NotFound();

            await _profileRepository.DeleteAsync(profile);
            return NoContent();
        }

        // Helper
        private EncodingProfileWithFormatsDto MapToDto(EncodingProfile profile)
        {
            return new EncodingProfileWithFormatsDto
            {
                Id = profile.Id,
                ProfileName = profile.ProfileName,
                Resolutions = profile.Resolutions,
                BitratesKbps = profile.BitratesKbps,
                Formats = profile.Formats.Select(f => new FormatDto
                {
                    Id = f.Id,
                    FormatType = f.FormatType
                }).ToList()
            };
        }
        [HttpGet("profiles")]
        public async Task<ActionResult<List<EncodingProfileWithFormatsDto>>> GetAllProfiles()
        {
            var profiles = await _profileRepository.GetAllAsync(); // Add this to your repository if it doesn’t exist
            return Ok(profiles.Select(p => MapToDto(p)).ToList());
        }

    }
}
