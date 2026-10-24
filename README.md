# MisskeyPoster
A tool for posting on Misskey, including posts with images.

## Build and run

Requires a stable .NET 10 SDK, or Docker. SDK selection permits newer .NET 10
feature bands and security patches while staying on .NET 10.

```sh
dotnet build MisskeyPoster.sln -c Release
docker build --pull -t misskeyposter:local .
docker run --rm -p 127.0.0.1:8080:8080 \
  --read-only \
  --tmpfs /tmp:rw,noexec,nosuid,size=64m,mode=1777 \
  --tmpfs /media:rw,noexec,nosuid,size=128m,uid=1654,gid=1654,mode=0700 \
  --cap-drop=ALL --security-opt=no-new-privileges \
  -e MISSKEY_HOST=misskey.io \
  misskeyposter:local
```

`GET /` returns `Hello, World!`. Swagger is available at `/swagger` only when
`ASPNETCORE_ENVIRONMENT=Development`.

## Container security and updates

- The build uses the official .NET 10 Ubuntu 24.04 SDK image. The runtime uses
  `10.0-noble-chiseled`, which omits the shell and package manager.
- The app runs as the unprivileged `app` user (UID/GID 1654). Application binaries
  and configuration in `/app` remain root-owned. `/media` is the working directory
  for temporary image downloads; the content root remains `/app`.
- The example uses a read-only root filesystem, drops Linux capabilities, and
  limits temporary storage. Concurrent image requests share the `/media` limit;
  adjust it to expected image sizes and concurrency. Temporary files disappear
  when the container stops.
- Image tags track servicing updates. Rebuild regularly with `docker build --pull`
  and recreate running containers to apply .NET and OS security patches. Existing
  containers do not update automatically.
- Check direct and transitive dependencies with
  `dotnet list MisskeyPoster.sln package --vulnerable --include-transitive` and scan
  the built image with your deployment's container vulnerability scanner.

The service accepts Misskey tokens and downloads caller-supplied image URLs.
Keep access restricted to trusted callers, terminate HTTPS at a reverse proxy,
and restrict outbound network access if exposing it beyond a trusted network.
Container hardening does not add API authentication or protection against SSRF.

### Verification on 2026-10-24

The image `misskeyposter:net10-local` was built and tested on Linux ARM64 with
.NET/ASP.NET Core 10.0.13. Checks passed for Production startup, Swagger exclusion
in Production, Development OpenAPI generation for all five routes, non-root
execution, read-only `/app`, writable temporary storage, and image cleanup after
an upstream connection failure. No real Misskey post was sent; successful posting
to a live server and other CPU architectures were not tested.

NuGet reported no known vulnerabilities in direct or transitive dependencies.
Docker Scout 0.20.0 reported zero Critical/High findings and two Medium findings
in the base image's `glibc` package (`2.39-0ubuntu8.9`):

- [CVE-2026-89092](https://ubuntu.com/security/CVE-2026-89092)
- [CVE-2026-18374](https://ubuntu.com/security/CVE-2026-18374)

At verification time, Scout reported no fixed package version and Ubuntu listed
24.04 as vulnerable. These findings remain unresolved; rebuild and rescan when
the base image receives fixes. A clean dependency audit is not a guarantee that
the container or application has no vulnerabilities.

Exposure assessment: the inspected image contains no `nscd`, which is required
for CVE-2026-89092. No direct native interop or attacker-controlled `fopen` mode
was found in the application source for CVE-2026-18374. Dependency internals were
not exhaustively reviewed, so this is a limited assessment rather than proof
that the second finding is unreachable.
