using CloudinaryDotNet;

namespace EduMateBackend.Services;

public class CloudinaryService(IConfiguration configuration)
{
    
    public readonly Cloudinary Cloudinary = new(configuration.GetValue<string>("CloudinarySettings:CLOUDINARY_URL"))
    {
        Api =
        {
            Secure = true
        }
    };
}