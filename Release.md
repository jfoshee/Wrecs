# Releasing

Releases are built from `main` by `.github/workflows/release.yml`. The workflow tests the solution, publishes the package and symbols to NuGet.org, and creates a GitHub tag and release for the same commit.

Configure [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) once for the `jfoshee` NuGet.org account with this policy:

- Repository owner: `jfoshee`
- Repository: `Wrecs`
- Workflow file: `release.yml`
- Environment: leave blank

After the workflow is on `main`, release a version with one command:

```sh
gh workflow run release.yml --ref main -f version=1.2.3.4
```

The version must have exactly four numeric parts and must not include the `v` prefix. The example publishes NuGet package `Wrecs` version `1.2.3.4` and creates GitHub tag and release `v1.2.3.4`. The same operation is also available under **Actions → Release → Run workflow** on GitHub.
