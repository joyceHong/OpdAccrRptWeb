# C# Coding Standards

This document defines the C# naming conventions used in this project.

## General Principles

- Use `PascalCase` and `camelCase` as the primary naming styles.
- Avoid separators such as underscores `_` or hyphens `-` in general names.
- Prefer readability over brevity.
  - Prefer `CanScrollHorizontally` over `ScrollableX`.
  - Prefer `HorizontalAlignment` over `AlignmentHorizontal`.
- Avoid Hungarian notation.
  - Do not use names such as `strName`.
- Use prefixes and suffixes only when they match the intended C# convention.
- Use abbreviations carefully.
  - For abbreviations of two letters or fewer, capitalize all letters, such as `IO`.
  - For abbreviations of three or more letters, use Pascal-style casing, such as `Xml`.
  - Avoid abbreviations longer than five letters.

## Naming Rules

| Item | Rule | Example | Notes |
| --- | --- | --- | --- |
| Project file | `PascalCase` | `Math.Algorithm.csproj` |  |
| Source file | `PascalCase` | `RuleSetup.cs` | Keep the source file name consistent with the class name. |
| Resource or embedded file | `PascalCase` | `TestPicture.jpg` |  |
| Namespace | `PascalCase` | `MyCompany.Wpf.Controls` | Prefer `CompanyName.ProjectNameOrTechnology.FeatureCategory.SubCategory`. |
| Class or struct | `PascalCase` | `CustomAttribute` | Use nouns. Use the base class name as a suffix when appropriate. |
| Interface | `PascalCase` with `I` prefix | `ICustomer` | Always use the `I` prefix. |
| Generic class or generic parameter type | `PascalCase` with `T` or `K` prefix | `TKey`, `TValue` | Use `T` or `K` prefixes. |
| Method | `PascalCase` | `ValidateUser` | Start with a verb. |
| Property | `PascalCase` | `Name` | Avoid `Get` or `Set` prefixes. |
| Public, protected, or internal field | `PascalCase` | `Name` |  |
| Private field | `_camelCase` | `_name` | Prefix with `_`. |
| Constant or static field | `PascalCase` | `Name` | Follow the same rule as fields. |
| Enum | `PascalCase` | `EncodeType` | Enum values also use `PascalCase`. |
| Delegate or event | `PascalCase` | `LoadPlugin` | Use `PascalCase` for all delegate and event names. |
| Local variable | `camelCase` | `string name` | Avoid single-character names and enum type names. |
| Parameter | `camelCase` | `Execute(string commandText, int iterations)` |  |

## Examples

```csharp
namespace MyCompany.Project.Users
{
    public interface IUserService
    {
        UserProfile GetUserProfile(string userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public UserProfile GetUserProfile(string userId)
        {
            UserProfile userProfile = _userRepository.FindById(userId);
            return userProfile;
        }
    }
}
```
