### Current User Information

The tool `getCurrentUser` returns information about the currently authenticated user:

- **UserId**: the numeric primary key of the user
- **UserLiteKey**: the Lite key (e.g. `UserEntity;42`)
- **UserName**: the login name of the user
- **UserRole**: the role assigned to the user
- **CurrentCulture**: the current culture (used for number and date formatting, e.g. `en-US`)
- **CurrentUICulture**: the current UI culture (used for translations, e.g. `es-ES`)

Use this when the user asks who they are logged in as, what their role is, or any question about their own identity or locale settings.
