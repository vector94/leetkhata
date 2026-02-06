# LeetKhata

> Automatically syncs your accepted LeetCode solutions to GitHub.

## How It Works

```
LeetCode (GraphQL API) → LeetKhata → GitHub (Git Tree API)
```

**Fetching** — Authenticates with your LeetCode session cookie & CSRF token, queries the GraphQL API for recent accepted submissions, then pulls solution code + problem metadata (difficulty, topics, stats).

**Tracking** — Reads `.leetkhata/sync-state.json` from your repo to skip already-synced submissions.

**Organizing** — Groups solutions by difficulty, each in its own folder:
```
Easy/1. Two Sum/
├── README.md       ← problem link, runtime stats, topics, submission link
└── solution.cpp
```

**Committing** — Uses GitHub's Git Tree API to push all files in a single atomic commit — everything goes in or nothing does.

**Scheduling** — Runs daily via GitHub Actions, or manually on demand.

## Setup

### 1. Get your LeetCode cookies

#### Option A: Automatic (recommended)

```bash
# One-time setup
pip install -r scripts/requirements.txt

# Refresh cookies and update GitHub secrets
python3 scripts/refresh-cookies.py --both
```

The script reads cookies directly from Chrome — just make sure you're logged into LeetCode in your browser.

#### Option B: Manual

Log in to [leetcode.com](https://leetcode.com) → DevTools (F12) → Application → Cookies → copy `LEETCODE_SESSION` and `csrftoken`.

> Cookies expire every 2-4 weeks. Re-run the script or repeat the manual process when they do.

### 2. Create a GitHub PAT

[GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens) → generate with `repo` scope.

### 3. Run locally

```bash
# Extract LeetCode cookies from Chrome into .env
python3 scripts/refresh-cookies.py

# Add your other config to .env (one-time)
# LEETKHATA__GitHubToken=your_github_pat
# LEETKHATA__LeetCodeUsername=your_leetcode_username
# LEETKHATA__GitHubOwner=your_github_username

dotnet run --project src/LeetKhata
```

The app reads configuration from `.env` automatically. Environment variables override `.env` if both are set.

### 4. Deploy with GitHub Actions

1. Push this repo to GitHub
2. **Settings → Secrets and variables → Actions**
3. Add **Secrets**: `LEETCODE_SESSION`, `LEETCODE_CSRF_TOKEN`, `GH_PAT`
4. Add **Variables**: `LEETCODE_USERNAME`, `GITHUB_OWNER`, `GITHUB_REPO`

Runs daily at 6:00 AM UTC. Trigger manually from the Actions tab anytime.

## Cookie Refresh

LeetCode cookies expire every 2-4 weeks. Use the helper script to refresh them:

```bash
python3 scripts/refresh-cookies.py              # Save to .env (default)
python3 scripts/refresh-cookies.py --github    # Update GitHub Actions secrets
python3 scripts/refresh-cookies.py --both      # .env + GitHub secrets
python3 scripts/refresh-cookies.py --repo owner/repo  # Override auto-detected repo
```

**Prerequisites:** `pip install -r scripts/requirements.txt` and `gh auth login` (for `--github`/`--both`). Requires Chrome with an active LeetCode login.

## Configuration

Non-secret settings in `src/LeetKhata/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `FetchLimit` | `20` | Recent submissions to check per run |
| `GitHubBranch` | `main` | Target branch in the solutions repo |

## License

MIT