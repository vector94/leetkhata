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

Log in to [leetcode.com](https://leetcode.com) → DevTools (F12) → Application → Cookies → copy `LEETCODE_SESSION` and `csrftoken`. These expire every 2-4 weeks.

### 2. Create a GitHub PAT

[GitHub Settings → Developer settings → Personal access tokens](https://github.com/settings/tokens) → generate with `repo` scope.

### 3. Run locally

```bash
export LEETKHATA__LeetCodeSession="your_session"
export LEETKHATA__LeetCodeCsrfToken="your_csrf_token"
export LEETKHATA__GitHubToken="your_github_pat"
export LEETKHATA__LeetCodeUsername="your_leetcode_username"
export LEETKHATA__GitHubOwner="your_github_username"
export LEETKHATA__GitHubRepo="leetcode-solutions"

dotnet run --project src/LeetKhata
```

### 4. Deploy with GitHub Actions

1. Push this repo to GitHub
2. **Settings → Secrets and variables → Actions**
3. Add **Secrets**: `LEETCODE_SESSION`, `LEETCODE_CSRF_TOKEN`, `GH_PAT`
4. Add **Variables**: `LEETCODE_USERNAME`, `GITHUB_OWNER`, `GITHUB_REPO`

Runs daily at 6:00 AM UTC. Trigger manually from the Actions tab anytime.

## Configuration

Non-secret settings in `src/LeetKhata/appsettings.json`:

| Setting | Default | Description |
|---------|---------|-------------|
| `FetchLimit` | `20` | Recent submissions to check per run |
| `GitHubBranch` | `main` | Target branch in the solutions repo |

## License

MIT