using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
namespace AVT.AWS.LambdaFn
{
    public class ApiGatewaySimpleAuthorization
    {
        public ApiGatewaySimpleAuthorization()
        {

        }

       public Policy FunctionHandler(APIGatewayCustomAuthorizerRequest authEvent, ILambdaContext context)
        {
            try
            {
                // validate the token -- SIMPLY accept a silly tokens ;)
                var token = authEvent.AuthorizationToken;
                Policy policy;
                if (CheckAuthorization(token) && "get".Equals(authEvent.HttpMethod, StringComparison.OrdinalIgnoreCase))
                {
                    var policyStatementList = new List<Statement>();
                    var policyStatement = new Statement(Statement.StatementEffect.Allow);
                    var actionIdentifier = SNSActionIdentifiers.Publish; // ;)

                    policyStatement.Actions.Add(actionIdentifier);

                    policyStatement.Principals.Add(Principal.AllUsers);
                    var resource = new Resource("arn:aws:lambda:eu-west-1:166778461577:function:TriggerNotification");
                    policyStatement.Resources.Add(resource);
                    policyStatementList.Add(policyStatement);


                    // Add conditions
                    var condition = ConditionFactory.NewSourceArnCondition("*");
                    policyStatement.Conditions.Add(condition);
                    policy = new Policy("CallNotifierLambdaPolicy", policyStatementList);
                }
                else
                {
                    var policyStatementList = new List<Statement>();
                    var policyStatement = new Statement(Statement.StatementEffect.Deny);
                    var actionIdentifier = SNSActionIdentifiers.AllSNSActions;
                    policyStatement.Actions.Add(actionIdentifier);

                    policy = new Policy("AccessDeniedPolicy", policyStatementList);
                }

                return policy;

            }
            catch (Exception e)
            {
                Console.WriteLine("Error authorizing request. " + e.Message);
                throw;
            }

        }
        public virtual bool CheckAuthorization(string token)
        {
            return token == "avt_2018test" || token == "bearer avt_2018test";
        }
    }

}
