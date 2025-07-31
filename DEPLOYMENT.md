# Deployment Guide

This guide explains how to use the GitHub Actions workflows for building, testing, and publishing the GenAI.Bridge NuGet package to GitHub Packages.

## Workflows Overview

### 1. Publish NuGet Package (`publish.yml`)

This is the main workflow for publishing packages. It can be triggered manually and includes:

- **Manual Trigger**: Run from GitHub Actions UI with optional parameters
- **Build & Test**: Compiles the project and runs all tests
- **Package Creation**: Creates NuGet package with proper metadata
- **GitHub Packages Publishing**: Publishes to GitHub's NuGet feed
- **Release Creation**: Creates GitHub releases with package attachments
- **Prerelease Support**: Optional prerelease mode with custom suffixes

### 2. Deploy Package (`deploy.yml`)

This workflow handles deployment events and provides deployment status tracking.

## How to Publish a Package

### Step 1: Prepare for Release

1. Ensure all tests pass locally:
   ```bash
   dotnet test
   ```

2. Update version information in your project (if needed)

3. Commit and push your changes:
   ```bash
   git add .
   git commit -m "Prepare for release"
   git push
   ```

### Step 2: Trigger the Workflow

1. Go to your GitHub repository
2. Navigate to **Actions** tab
3. Select **Publish NuGet Package** workflow
4. Click **Run workflow**
5. Configure the parameters:
   - **prerelease**: Check if this is a prerelease version
   - **version_suffix**: Optional custom suffix (e.g., "beta", "alpha", "rc")

### Step 3: Monitor the Process

The workflow will:
1. ✅ Build the project
2. ✅ Run all tests
3. ✅ Create NuGet package
4. ✅ Publish to GitHub Packages
5. ✅ Create GitHub release
6. ✅ Upload package to release

## Package Versioning

### Regular Releases
- Version is based on the latest git tag
- If no tag exists, defaults to `0.1.0`
- Example: `1.2.3`

### Prereleases
- Adds `-prerelease` suffix by default
- Example: `1.2.3-prerelease`
- Can use custom suffix via `version_suffix` parameter
- Examples: `1.2.3-beta`, `1.2.3-alpha`, `1.2.3-rc`

## GitHub Packages Configuration

The package will be published to GitHub Packages with the following configuration:

- **Package ID**: `GenAI.Bridge`
- **Feed URL**: `https://nuget.pkg.github.com/{owner}/index.json`
- **Authentication**: Uses `GITHUB_TOKEN` (automatically provided)

### Consuming the Package

Add the GitHub Packages source to your project:

```xml
<PackageSources>
  <add key="github" value="https://nuget.pkg.github.com/{owner}/index.json" />
</PackageSources>
```

Or via command line:
```bash
dotnet nuget add source https://nuget.pkg.github.com/{owner}/index.json --name github
```

Then install the package:
```bash
dotnet add package GenAI.Bridge
```

## Environment Protection

The workflow uses GitHub's deployment environment feature:

- **Environment**: `production`
- **Protection**: Requires review (configured in `.github/environments/production.yml`)
- **Deployment Tracking**: Full deployment status tracking

## Troubleshooting

### Common Issues

1. **Authentication Errors**
   - Ensure the repository has proper permissions
   - Check that `GITHUB_TOKEN` is available

2. **Package Already Exists**
   - The workflow uses `--skip-duplicate` flag
   - If you need to republish, manually delete the package first

3. **Version Conflicts**
   - Ensure your git tags are properly formatted (e.g., `v1.2.3`)
   - Check that the version doesn't already exist

4. **Test Failures**
   - Fix any failing tests before publishing
   - The workflow will fail if tests don't pass

### Debugging

- Check the workflow logs in the Actions tab
- Verify package metadata in `GenAI.Bridge.csproj`
- Ensure all required files are present (README.md, icon.png)

## Security Considerations

- The workflow uses `GITHUB_TOKEN` for authentication
- Packages are published to GitHub Packages (private by default)
- Environment protection requires review for production deployments
- All dependencies are pinned to specific versions

## Customization

### Modifying Package Metadata

Edit `GenAI.Bridge/GenAI.Bridge.csproj` to update:
- Package description
- Authors and company information
- License type
- Tags and categories

### Adding Additional Steps

The workflow can be extended to include:
- Code coverage reporting
- Security scanning
- Documentation generation
- Additional validation steps

## Support

For issues with the deployment process:
1. Check the workflow logs
2. Verify repository permissions
3. Ensure all required files are present
4. Contact the repository maintainers
