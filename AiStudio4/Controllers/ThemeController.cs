// Controllers/ThemeController.cs
using AiStudio4.Core.Interfaces;
using AiStudio4.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AiStudio4.Controllers
{
    [ApiController]
    [Route("api/themes")]
    public class ThemeController : ControllerBase
    {
        private readonly IThemeService _themeService;

        public ThemeController(IThemeService themeService)
        {
            _themeService = themeService;
        }

        /// <summary>
        /// Get all available themes.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<Theme>>> GetAllThemes()
        {
            var themes = await _themeService.GetAllThemesAsync();
            return Ok(themes);
        }

        /// <summary>
        /// Get a theme by its ID.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Theme>> GetThemeById(string id)
        {
            var theme = await _themeService.GetThemeByIdAsync(id);
            if (theme == null) return NotFound();
            return Ok(theme);
        }

        /// <summary>
        /// Add a new theme to the library.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Theme>> AddTheme([FromBody] Theme theme)
        {
            if (theme == null)
                return BadRequest("Theme data is required.");

            var added = await _themeService.AddThemeAsync(theme);
            return CreatedAtAction(nameof(GetThemeById), new { id = added.Id }, added);
        }

        /// <summary>
        /// Delete a theme by its ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTheme(string id)
        {
            var deleted = await _themeService.DeleteThemeAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Import themes from a JSON string.
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<List<Theme>>> ImportThemes([FromBody] string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return BadRequest("JSON data is required.");

            var imported = await _themeService.ImportThemesAsync(json);
            return Ok(imported);
        }

        /// <summary>
        /// Export selected themes as a JSON string.
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult<string>> ExportThemes([FromBody] List<string> themeIds)
        {
            if (themeIds == null || themeIds.Count == 0)
                return BadRequest("Theme IDs are required.");

            var json = await _themeService.ExportThemesAsync(themeIds);
            return Ok(json);
        }

        /// <summary>
        /// Sets a theme as the default theme.
        /// </summary>
        [HttpPost("setDefault/{id}")]
        public async Task<IActionResult> SetDefaultTheme(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Theme ID is required.");

            var success = await _themeService.SetDefaultThemeAsync(id);
            if (!success) return NotFound("Theme not found or could not be set as default.");
            return Ok();
        }

        /// <summary>
        /// Gets the current default theme.
        /// </summary>
        [HttpGet("default")]
        public async Task<ActionResult<Theme>> GetDefaultTheme()
        {
            var theme = await _themeService.GetDefaultThemeAsync();
            if (theme == null) return NotFound("No default theme set.");
            return Ok(theme);
        }
    }
}