// Copyright (c) Microsoft. All rights reserved.

using Microsoft.SemanticKernel;
using System.Net;

internal static class KernelBuilderExtensions
{
    /// <summary>
    /// Adds a text completion service to the list. It can be either an OpenAI or Azure OpenAI backend service.
    /// </summary>
    /// <param name="kernelBuilder"></param>
    /// <exception cref="ArgumentException"></exception>
    internal static KernelBuilder WithCompletionService(this KernelBuilder kernelBuilder)
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
                modelId: Env.Var("AzureOpenAI:TextCompletionModelId")!,
                endpoint: Env.Var("AzureOpenAI:Endpoint")!,
                serviceId: "AzureOpenAIChat",
                apiKey: Env.Var("AzureOpenAI:ApiKey")!)
            .Build();

        return kernelBuilder;
    }
}