#!/usr/bin/env python3
"""
LeetKhata Cookie Refresh Helper

Reads LeetCode cookies directly from your Chrome browser
and optionally updates your GitHub Actions secrets.

Prerequisites:
    pip install -r scripts/requirements.txt
    gh auth login  (only if using --github or --both)
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import urllib.request


def extract_cookies_from_chrome():
    """Extract LEETCODE_SESSION and csrftoken from Chrome's cookie store."""
    try:
        import browser_cookie3
    except ImportError:
        print("Error: browser_cookie3 is not installed.")
        print("Run: pip install -r scripts/requirements.txt")
        sys.exit(1)

    print("Reading cookies from Chrome...")

    try:
        cookiejar = browser_cookie3.chrome(domain_name="leetcode.com")
    except Exception as e:
        print(f"Error reading Chrome cookies: {e}")
        print("\nMake sure Chrome is installed and you're logged into leetcode.com.")
        print("You may need to close Chrome first on some systems.")
        sys.exit(1)

    leetcode_session = None
    csrf_token = None

    for cookie in cookiejar:
        if cookie.name == "LEETCODE_SESSION":
            leetcode_session = cookie.value
        if cookie.name == "csrftoken":
            csrf_token = cookie.value

    if not leetcode_session or not csrf_token:
        print("Error: Could not find LeetCode cookies in Chrome.")
        print("Make sure you're logged into https://leetcode.com in Chrome.")
        sys.exit(1)

    print("Cookies extracted successfully.")
    return leetcode_session, csrf_token


def check_gh_cli():
    """Check that gh CLI is installed and authenticated."""
    if not shutil.which("gh"):
        print("Error: GitHub CLI (gh) is not installed.")
        print("Install it from: https://cli.github.com/")
        return False

    result = subprocess.run(
        ["gh", "auth", "status"], capture_output=True, text=True
    )
    if result.returncode != 0:
        print("Error: GitHub CLI is not authenticated.")
        print("Run: gh auth login")
        return False

    return True


def get_github_repo_info(override=None):
    """Parse owner/repo from git remote origin URL or use override."""
    if override:
        parts = override.split("/")
        if len(parts) != 2:
            print(f"Error: Invalid repo format '{override}'. Expected 'owner/repo'.")
            sys.exit(1)
        return parts[0], parts[1]

    try:
        result = subprocess.run(
            ["git", "remote", "get-url", "origin"],
            capture_output=True,
            text=True,
            check=True,
        )
        url = result.stdout.strip()
        # Handle HTTPS and SSH formats
        match = re.search(r"github\.com[:/](.+?)/(.+?)(?:\.git)?$", url)
        if not match:
            print(f"Error: Could not parse GitHub owner/repo from: {url}")
            print("Use --repo owner/repo to specify manually.")
            sys.exit(1)
        return match.group(1), match.group(2)
    except subprocess.CalledProcessError:
        print("Error: Could not get git remote URL.")
        print("Use --repo owner/repo to specify manually.")
        sys.exit(1)


def validate_cookies(session, csrf):
    """Test cookies against LeetCode API, return username or None."""
    query = json.dumps(
        {
            "query": "query { userStatus { username } }",
            "variables": {},
        }
    ).encode()

    req = urllib.request.Request(
        "https://leetcode.com/graphql/",
        data=query,
        headers={
            "Content-Type": "application/json",
            "Cookie": f"LEETCODE_SESSION={session}; csrftoken={csrf}",
            "x-csrftoken": csrf,
            "Referer": "https://leetcode.com",
            "User-Agent": "LeetKhata/1.0",
        },
    )

    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = json.loads(resp.read())
            username = (
                data.get("data", {}).get("userStatus", {}).get("username")
            )
            if username:
                return username
    except Exception:
        pass
    return None


def update_github_secrets(owner, repo, session, csrf):
    """Set LEETCODE_SESSION and LEETCODE_CSRF_TOKEN via gh CLI."""
    secrets = {
        "LEETCODE_SESSION": session,
        "LEETCODE_CSRF_TOKEN": csrf,
    }

    for name, value in secrets.items():
        try:
            subprocess.run(
                ["gh", "secret", "set", name, "--repo", f"{owner}/{repo}"],
                input=value,
                text=True,
                check=True,
                capture_output=True,
            )
            print(f"  Updated secret: {name}")
        except subprocess.CalledProcessError as e:
            print(f"  Error updating {name}: {e.stderr.strip()}")
            sys.exit(1)


def get_project_root():
    """Find the project root (where .sln or .git lives)."""
    result = subprocess.run(
        ["git", "rev-parse", "--show-toplevel"],
        capture_output=True, text=True
    )
    if result.returncode == 0:
        return result.stdout.strip()
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def write_env_file(session, csrf):
    """Write cookies to .env file in the project root."""
    env_path = os.path.join(get_project_root(), ".env")

    # Read existing .env to preserve other variables
    existing = {}
    if os.path.exists(env_path):
        with open(env_path, "r") as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, _, value = line.partition("=")
                    existing[key.strip()] = value.strip()

    existing["LEETKHATA__LeetCodeSession"] = session
    existing["LEETKHATA__LeetCodeCsrfToken"] = csrf

    with open(env_path, "w") as f:
        for key, value in existing.items():
            f.write(f'{key}={value}\n')

    print(f"  Updated: {env_path}")


def mask(value):
    """Mask a token value for display."""
    if len(value) <= 10:
        return "***"
    return value[:8] + "..." + value[-4:]


def main():
    parser = argparse.ArgumentParser(
        description="Refresh LeetCode cookies for LeetKhata."
    )
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--local",
        action="store_true",
        default=True,
        help="Save cookies to .env file (default)",
    )
    mode.add_argument(
        "--github",
        action="store_true",
        help="Update GitHub Actions secrets via gh CLI",
    )
    mode.add_argument(
        "--both",
        action="store_true",
        help="Save to .env and update GitHub secrets",
    )
    parser.add_argument(
        "--repo",
        metavar="OWNER/REPO",
        help="Override auto-detected GitHub repository",
    )
    args = parser.parse_args()

    need_gh = args.github or args.both

    # Check prerequisites
    if need_gh and not check_gh_cli():
        return 1

    # Detect repo info
    owner, repo = None, None
    if need_gh:
        owner, repo = get_github_repo_info(args.repo)
        print(f"Target repository: {owner}/{repo}")

    # Extract cookies from Chrome
    session, csrf = extract_cookies_from_chrome()

    # Validate cookies
    username = validate_cookies(session, csrf)
    if username:
        print(f"Verified: logged in as '{username}'")
    else:
        print("Warning: Could not verify cookies (they may still work).")

    # Output
    if args.both or (not args.github):
        print("\nSaving cookies to .env file...")
        write_env_file(session, csrf)

    if need_gh:
        print(f"\nUpdating GitHub secrets for {owner}/{repo}...")
        update_github_secrets(owner, repo, session, csrf)

    # Summary
    print("\n" + "=" * 60)
    print("COOKIES REFRESHED SUCCESSFULLY")
    print("=" * 60)
    print(f"  LEETCODE_SESSION: {mask(session)}")
    print(f"  csrftoken:        {mask(csrf)}")
    if username:
        print(f"  Logged in as:     {username}")
    if args.both or (not args.github):
        print(f"  .env file:        Updated")
    if need_gh:
        print(f"  GitHub secrets:   Updated ({owner}/{repo})")
    print("=" * 60)

    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("\nCancelled.")
        sys.exit(0)
