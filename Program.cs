using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynSecurityGuard.Analyzers;
using RoslynSecurityGuard.Analyzers.Taint;
using RoslynSecurityGuard.Helpers;

namespace RoslynSecurityGuard
{
    public class SourceVerifier : DiagnosticVerifier
    {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            return new DiagnosticAnalyzer[] {
                // Many false positives
                // new CsrfTokenAnalyzer(),

                new TaintAnalyzer(),
                new InsecureCookieAnalyzer(),
                new OutputCacheAnnotationAnalyzer(),
                new RequestValidationAnalyzer(),
                new UnknownPasswordApiAnalyzer(),
                new WeakCertificateValidationAnalyzer(),
                new WeakCipherAnalyzer(),
                new WeakCipherModeAnalyzer(),
                new WeakHashingAnalyzer(),
                new WeakPasswordValidatorPropertyAnalyzer(),
                new WeakRandomAnalyzer(),
                new WebConfigAnalyzer(),
                new XssPreventionAnalyzer(),
                new XxeAnalyzer(),
            };
        }

        protected override IEnumerable<MetadataReference> GetAdditionnalReferences()
        {
            return null;
        }

        public async Task Execute(string path)
        {
            await VerifyCSharpDiagnostic(path);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SourceVerifier verifier = new SourceVerifier();
            verifier.Execute(args[0]).GetAwaiter().GetResult();
        }
    }
}
