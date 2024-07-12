﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LocalEmbeddings {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class LlmPrompts {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LlmPrompts() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("LocalEmbeddings.LlmPrompts", typeof(LlmPrompts).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to I&apos;m going to give you document, and I need you to answer the following question.
        /// </summary>
        internal static string PromptToAnswerQuestionAboutDocument {
            get {
                return ResourceManager.GetString("PromptToAnswerQuestionAboutDocument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to I&apos;m going to give you set of issues, and I need you to answer the following question about them.
        /// </summary>
        internal static string PromptToAnswerQuestionAboutSummary {
            get {
                return ResourceManager.GetString("PromptToAnswerQuestionAboutSummary", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are going to be given github issue, which specifies a feature or describes a bug for an educational software package called maths pathway. You are required to summarise it for later searching. You need to include the names of the affected parts of the system and a short but detailed summary of the things that are being changed in the ticket. Try as hard as possible to include all detail without including extraneous or generic details..
        /// </summary>
        internal static string PromptToSummariseDocument {
            get {
                return ResourceManager.GetString("PromptToSummariseDocument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are a helpful assistant who specialises in answering questions about design documents, which include details of features for an educational software package. Answer the question as best you can with the details in the issue, as succinctly as possible, without adding anything you are unsure about.
        /// </summary>
        internal static string SystemMessageBeforeAnsweringQuestions {
            get {
                return ResourceManager.GetString("SystemMessageBeforeAnsweringQuestions", resourceCulture);
            }
        }
    }
}
