namespace EduMateBackend.Helpers;

public enum Errors
{
    DuplicateUsername,
    DuplicateEmail,
    None,
    UserNotFound,
    UserAlreadyFollowed,
    UserNotFollowed,
    Unauthorized,
    UnknownError,
    PctNotFound,
    IncorrectPasskey,
    PasskeyTooOld
}