namespace LeetKhata.LeetCode;

public static class Queries
{
    public const string SubmissionList = """
        query ($offset: Int!, $limit: Int!, $slug: String) {
            submissionList(offset: $offset, limit: $limit, questionSlug: $slug) {
                hasNext
                submissions {
                    id
                    lang
                    time
                    timestamp
                    statusDisplay
                    runtime
                    url
                    isPending
                    title
                    memory
                    titleSlug
                }
            }
        }
        """;

    public const string SubmissionDetails = """
        query submissionDetails($id: Int!) {
            submissionDetails(submissionId: $id) {
                id
                code
                timestamp
                statusCode
                runtimeDisplay
                runtimePercentile
                memoryDisplay
                memoryPercentile
                lang {
                    name
                    verboseName
                }
                question {
                    questionId
                    titleSlug
                }
                topicTags {
                    name
                    slug
                }
            }
        }
        """;

    public const string QuestionData = """
        query selectProblem($titleSlug: String!) {
            question(titleSlug: $titleSlug) {
                questionId
                questionFrontendId
                title
                titleSlug
                content
                difficulty
                topicTags {
                    name
                    slug
                }
            }
        }
        """;
}
