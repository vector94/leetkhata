namespace LeetKhata.LeetCode;

public class LeetCodeSessionExpiredException : Exception
{
    public LeetCodeSessionExpiredException()
        : base(BuildMessage())
    {
    }

    private static string BuildMessage() => """

        ============================================================
        LEETCODE SESSION EXPIRED
        ============================================================
        Your LeetCode cookies have expired. To fix this:

        1. Log in to https://leetcode.com in your browser
        2. Open DevTools (F12) > Application > Cookies
        3. Copy the new values for:
           - LEETCODE_SESSION
           - csrftoken
        4. Update your environment variables or GitHub Actions secrets:
           - LEETKHATA__LeetCodeSession  (or secret LEETCODE_SESSION)
           - LEETKHATA__LeetCodeCsrfToken (or secret LEETCODE_CSRF_TOKEN)

        TIP: Run 'python3 scripts/refresh-cookies.py --both' to automate
        cookie capture and GitHub secrets update.

        This typically needs to be done every 2-4 weeks.
        ============================================================
        """;
}
