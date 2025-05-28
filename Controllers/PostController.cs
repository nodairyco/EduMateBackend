using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EduMateBackend.Helpers;
using EduMateBackend.Models;
using EduMateBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduMateBackend.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class PostController(PostService postService) : ControllerBase
{
    private readonly PostService _postService = postService;

    [HttpPost("/addPost")]
    [Authorize(Policy = "VerifiedOnly")]
    public async Task<ActionResult<Post>> AddPostToUsersFeedAsync([MaxLength(1000)] string content, IFormFile? file1,
        IFormFile? file2,
        IFormFile? file3, IFormFile? file4)
    {
        ICollection<IFormFile> attachmentList = new List<IFormFile>();
        if (file1 != null)
        {
            attachmentList.Add(file1);
            if (file2 != null)
            {
                attachmentList.Add(file2);
                if (file3 != null)
                {
                    attachmentList.Add(file3);
                    if (file4 != null)
                    {
                        attachmentList.Add(file4);
                    }
                }
            }
        }

        IFormFileCollection attachments = (attachmentList.Count == 0 ? [] : attachmentList as FormFileCollection)!;

        var uploaderGuid = (HttpContext.User.Identity as ClaimsIdentity)!.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var postParent = new PostParent { ParentId = uploaderGuid, ParentType = PostParent.PostParentType.User };

        var result = await _postService.UploadPostAsync(content, new Guid(uploaderGuid), attachments, postParent);

        return result.Item1 switch
        {
            Errors.UnknownError => BadRequest("Unknown Error occured, aborted"),
            _ => Ok(result.Item2)
        };
    }
}