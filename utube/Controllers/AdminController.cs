using Microsoft.AspNetCore.Mvc;
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
            var profiles = await _profileRepository.GetAllAsync();
            return Ok(profiles.Select(p => MapToDto(p)).ToList());
        }

    }
}
