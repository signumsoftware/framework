# Creating Signum Upgrades

When creating `Signum.Upgrade` scripts from Southwind commits:

- Ignore changes that are only Framework submodule pointer updates.
- Include Framework-related updates only when the corresponding changes are also present in Southwind files.
