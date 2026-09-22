using Amazon.Lambda.APIGatewayEvents;
using static Amazon.Lambda.APIGatewayEvents.APIGatewayCustomAuthorizerPolicy;

namespace PingLight.Core.Auth
{
    public static class AuthPolicyGenerator
    {
        public static APIGatewayCustomAuthorizerResponse GeneratePolicy(string principalId, string effect, string resource)
        {
            return new APIGatewayCustomAuthorizerResponse
            {
                PrincipalID = principalId,
                PolicyDocument = new APIGatewayCustomAuthorizerPolicy
                {
                    Version = "2012-10-17",
                    Statement = new List<IAMPolicyStatement>
                    {
                        new IAMPolicyStatement
                        {
                            Action = new HashSet<string> { "execute-api:Invoke" },
                            Effect = effect,
                            Resource = new HashSet<string> { resource }
                        }
                    }
                }
            };
        }
    }
}
