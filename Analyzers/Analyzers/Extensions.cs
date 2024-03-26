using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;

namespace Skyline.DataMiner.Utils.SecureCoding.Analyzers
{
    public static class Extensions
    {
        /// <summary>
        /// Retrieves the full name of a namespace represented by the provided INamespaceSymbol object.
        /// </summary>
        /// <param name="ns">The namespace symbol for which to retrieve the full name.</param>
        /// <returns>
        /// Returns the full name of the namespace represented by the INamespaceSymbol. 
        /// If the provided namespace symbol is null, the method returns null.
        /// If the namespace symbol represents the global namespace, an empty string is returned.
        /// If the namespace symbol is not the global namespace, the method recursively constructs 
        /// the full name by appending the current namespace's name to the full name of its containing namespace, 
        /// separated by a period.
        /// </returns>
        public static string GetNamespaceFullname(this INamespaceSymbol ns)
        {
            if (ns == null)
            {
                return null;
            }

            if (ns.IsGlobalNamespace)
            {
                return "";
            }

            if (ns.ContainingNamespace.IsGlobalNamespace)
            {
                return ns.Name;
            }

            return GetNamespaceFullname(ns.ContainingNamespace) + "." + ns.Name;
        }

        /// <summary>
        /// Retrieves the name of an assignment represented by the provided AssignmentExpressionSyntax object.
        /// </summary>
        /// <param name="assignment">The assignment expression syntax to retrieve the name for.</param>
        /// <returns>
        /// Returns the name of the assignment. 
        /// For regular cases (variables/properties assignments not within object initializers), 
        /// it returns the name of the left-hand side of the assignment. 
        /// For assignments within object initializers, it returns the concatenation of the ancestor 
        /// variable declarator's identifier and the name of the left-hand side of the assignment, 
        /// separated by a period. 
        /// If the assignment is not within an object initializer and no ancestor variable declarator 
        /// is found, it behaves the same as the regular case.
        /// </returns>
        public static string GetAssignmentName(this AssignmentExpressionSyntax assignment)
        {
            // Regular case => Variables/Properties assignments
            if (!assignment.Parent.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                return assignment.Left.ToString();
            }

            // Special case => Object Initializations
            var ancestorVariableDeclarator = assignment
                .FirstAncestorOrSelf<VariableDeclaratorSyntax>(ascendOutOfTrivia: true);

            if (ancestorVariableDeclarator != null)
            {
                return $"{ancestorVariableDeclarator.Identifier}.{assignment.Left}";
            }

            // Regular case => Variables/Properties assignments
            return assignment.Left.ToString();
        }

        /// <summary>
        /// Determines whether the specified location occurs after another location in the source code.
        /// </summary>
        /// <param name="location">The location to check for posteriority.</param>
        /// <param name="otherLocation">The other location to compare against.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the specified location comes after the other location in the source code; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsPosteriorLocation(this Location location, Location otherLocation)
        {
            // Check if location comes after other location
            return location.SourceSpan.Start > otherLocation.SourceSpan.End;
        }

        /// <summary>
        /// Determines whether the specified location is between two other locations in the source code.
        /// </summary>
        /// <param name="location">The location to check if it is between the start and end locations.</param>
        /// <param name="start">The start location.</param>
        /// <param name="end">The end location.</param>
        /// <returns>
        /// Returns <see langword="true"/> if the specified location is between the start and end locations (inclusive); otherwise, <see langword="false"/>.
        /// </returns>
        public static bool IsBetweenLocations(this Location location, Location start, Location end)
        {
            // Check if the location is between start and end
            return location.SourceSpan.Start >= start.SourceSpan.Start &&
                   location.SourceSpan.End <= end.SourceSpan.End;
        }
    }
}
