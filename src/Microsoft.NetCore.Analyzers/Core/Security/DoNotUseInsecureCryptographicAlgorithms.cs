// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.NetCore.Analyzers.Security.Helpers;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Security
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseInsecureCryptographicAlgorithmsAnalyzer : DiagnosticAnalyzer
    {
        internal const string DoNotUseWeakCryptographyRuleId = "CA5350";
        internal const string DoNotUseBrokenCryptographyRuleId = "CA5351";

        internal const string CA5350HelpLink = "https://docs.microsoft.com/visualstudio/code-quality/ca5350-do-not-use-weak-cryptographic-algorithms";
        internal const string CA5351HelpLink = "https://docs.microsoft.com/visualstudio/code-quality/ca5351-do-not-use-broken-cryptographic-algorithms";

        private static readonly LocalizableString s_localizableDoNotUseWeakAlgorithmsTitle = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWeakCryptographicAlgorithms),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDoNotUseWeakAlgorithmsMessage = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWeakCryptographicAlgorithmsMessage),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDoNotUseWeakAlgorithmsDescription = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWeakCryptographicAlgorithmsDescription),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDoNotUseBrokenAlgorithmsTitle = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseBrokenCryptographicAlgorithms),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDoNotUseBrokenAlgorithmsMessage = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseBrokenCryptographicAlgorithmsMessage),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDoNotUseBrokenAlgorithmsDescription = new LocalizableResourceString(
            nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseBrokenCryptographicAlgorithmsDescription),
            MicrosoftNetCoreAnalyzersResources.ResourceManager,
            typeof(MicrosoftNetCoreAnalyzersResources));

        internal static DiagnosticDescriptor DoNotUseBrokenCryptographyRule =
            new DiagnosticDescriptor(
                DoNotUseBrokenCryptographyRuleId,
                s_localizableDoNotUseBrokenAlgorithmsTitle,
                s_localizableDoNotUseBrokenAlgorithmsMessage,
                DiagnosticCategory.Security,
                DiagnosticHelpers.DefaultDiagnosticSeverity,
                isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
                description: s_localizableDoNotUseBrokenAlgorithmsDescription,
                helpLinkUri: CA5351HelpLink,
                customTags: WellKnownDiagnosticTags.Telemetry);

        internal static DiagnosticDescriptor DoNotUseWeakCryptographyRule =
            new DiagnosticDescriptor(
                DoNotUseWeakCryptographyRuleId,
                s_localizableDoNotUseWeakAlgorithmsTitle,
                s_localizableDoNotUseWeakAlgorithmsMessage,
                DiagnosticCategory.Security,
                DiagnosticHelpers.DefaultDiagnosticSeverity,
                isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
                description: s_localizableDoNotUseWeakAlgorithmsDescription,
                helpLinkUri: CA5350HelpLink,
                customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotUseBrokenCryptographyRule, DoNotUseWeakCryptographyRule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();

            // Security analyzer - analyze and report diagnostics on generated code.
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            analysisContext.RegisterCompilationStartAction(
                (CompilationStartAnalysisContext context) =>
                {
                    var cryptTypes = new CompilationSecurityTypes(context.Compilation);
                    if (!ReferencesAnyTargetType(cryptTypes))
                    {
                        return;
                    }

                    context.RegisterOperationAction(
                        (OperationAnalysisContext operationAnalysisContext) =>
                        {
                            IMethodSymbol method;

                            switch (operationAnalysisContext.Operation)
                            {
                                case IInvocationOperation invocationOperation:
                                    method = invocationOperation.TargetMethod;
                                    break;
                                case IObjectCreationOperation objectCreationOperation:
                                    method = objectCreationOperation.Constructor;
                                    break;
                                default:
                                    Debug.Fail($"Unhandled IOperation {operationAnalysisContext.Operation.Kind}");
                                    return;
                            }

                            INamedTypeSymbol type = method.ContainingType;
                            DiagnosticDescriptor? rule = null;
                            string? algorithmName = null;

                            if (type.DerivesFrom(cryptTypes.MD5))
                            {
                                rule = DoNotUseBrokenCryptographyRule;
                                algorithmName = cryptTypes.MD5.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.SHA1))
                            {
                                rule = DoNotUseWeakCryptographyRule;
                                algorithmName = cryptTypes.SHA1.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.HMACSHA1))
                            {
                                rule = DoNotUseWeakCryptographyRule;
                                algorithmName = cryptTypes.HMACSHA1.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.DES))
                            {
                                rule = DoNotUseBrokenCryptographyRule;
                                algorithmName = cryptTypes.DES.Name;
                            }
                            else if ((method.ContainingType.DerivesFrom(cryptTypes.DSA)
                                      && method.MetadataName == SecurityMemberNames.CreateSignature)
                                || (type.Equals(cryptTypes.DSASignatureFormatter)
                                    && method.ContainingType.DerivesFrom(cryptTypes.DSASignatureFormatter)
                                    && method.MetadataName == WellKnownMemberNames.InstanceConstructorName))
                            {
                                rule = DoNotUseBrokenCryptographyRule;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                algorithmName = cryptTypes.DSA.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                            }
                            else if (type.DerivesFrom(cryptTypes.HMACMD5))
                            {
                                rule = DoNotUseBrokenCryptographyRule;
                                algorithmName = cryptTypes.HMACMD5.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.RC2))
                            {
                                rule = DoNotUseBrokenCryptographyRule;
                                algorithmName = cryptTypes.RC2.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.TripleDES))
                            {
                                rule = DoNotUseWeakCryptographyRule;
                                algorithmName = cryptTypes.TripleDES.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.RIPEMD160))
                            {
                                rule = DoNotUseWeakCryptographyRule;
                                algorithmName = cryptTypes.RIPEMD160.Name;
                            }
                            else if (type.DerivesFrom(cryptTypes.HMACRIPEMD160))
                            {
                                rule = DoNotUseWeakCryptographyRule;
                                algorithmName = cryptTypes.HMACRIPEMD160.Name;
                            }

                            if (rule != null)
                            {
                                operationAnalysisContext.ReportDiagnostic(
                                    Diagnostic.Create(
                                        rule,
                                        operationAnalysisContext.Operation.Syntax.GetLocation(),
                                        operationAnalysisContext.ContainingSymbol.Name,
                                        algorithmName));
                            }
                        },
                        OperationKind.Invocation,
                        OperationKind.ObjectCreation);
                });
        }

        private static bool ReferencesAnyTargetType(CompilationSecurityTypes types)
        {
            return types.MD5 != null
                || types.SHA1 != null
                || types.HMACSHA1 != null
                || types.DES != null
                || types.DSA != null
                || types.DSASignatureFormatter != null
                || types.HMACMD5 != null
                || types.RC2 != null
                || types.TripleDES != null
                || types.RIPEMD160 != null
                || types.HMACRIPEMD160 != null;
        }
    }
}

