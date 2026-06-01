# Security Policy

## Supported Versions

TimeKeeper is a newly public project. Security fixes are expected to target the latest released version unless the maintainer states otherwise.

## Reporting a Vulnerability

Please report security issues privately to the maintainer rather than opening a public issue.

Preferred contact:

- GitHub: https://github.com/Satoshi-sts

When reporting a vulnerability, include:

- A clear description of the issue
- Steps to reproduce, if available
- Impact and affected versions, if known
- Whether any credentials or private data may be involved

Please do not include public proof-of-concept details until the issue has been reviewed.

## Credential Policy

TimeKeeper must not embed GitHub Personal Access Tokens, passwords, signing keys, or other credentials in source code or release artifacts.

The in-app update source is the public release artifact repository `Satoshi-sts/TimeKeeper-Releases`. The source repository `Satoshi-sts/TimeKeeper` must not be used as the app update feed.
