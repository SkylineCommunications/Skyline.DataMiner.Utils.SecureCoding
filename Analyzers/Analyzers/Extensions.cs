using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;
using System;

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
        /// Gets the name of the assignment from the given assignment expression syntax.
        /// </summary>
        /// <param name="assignment">The assignment expression syntax.</param>
        /// <returns>
        /// The name of the assignment, which is typically the name of the variable or property being assigned to.
        /// For object initializer expressions, it returns the name prefixed with the variable's or property's identifier.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the assignment expression syntax is null.</exception>
        public static string GetAssignmentName(this AssignmentExpressionSyntax assignment)
        {
            if (assignment is null)
            {
                throw new ArgumentNullException(nameof(assignment));
            }

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
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <paramref name="location"/> or <paramref name="otherLocation"/> is null.
        /// </exception>
        public static bool IsPosteriorLocation(this Location location, Location otherLocation)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (otherLocation is null)
            {
                throw new ArgumentNullException(nameof(otherLocation));
            }

            // Check if location comes after other location
            return location.SourceSpan.Start > otherLocation.SourceSpan.End;
        }

        /// <summary>
        /// Determines if the specified location comes before another location.
        /// </summary>
        /// <param name="location">The location to compare.</param>
        /// <param name="otherLocation">The other location to compare against.</param>
        /// <returns>Returns <see langword="true"/> if the specified location comes before the other location; otherwise, <see langword="false"/>.
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <paramref name="location"/> or <paramref name="otherLocation"/> is null.
        /// </exception>
        public static bool IsAnteriorLocation(this Location location, Location otherLocation)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (otherLocation is null)
            {
                throw new ArgumentNullException(nameof(otherLocation));
            }

            // Check if location comes before other location
            return location.SourceSpan.Start < otherLocation.SourceSpan.End;
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
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="location"/>, <paramref name="start"/>, or <paramref name="end"/> is null.
        /// </exception>
        public static bool IsBetweenLocations(this Location location, Location start, Location end)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (start is null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (end is null)
            {
                throw new ArgumentNullException(nameof(end));
            }

            // Check if the location is between start and end
            return location.SourceSpan.Start >= start.SourceSpan.Start &&
                   location.SourceSpan.End <= end.SourceSpan.End;
        }

        /// <summary>
        /// Determines whether two locations in source code overlap with each other.
        /// </summary>
        /// <param name="location">The first location to compare.</param>
        /// <param name="otherLocation">The other location to compare against.</param>
        /// <returns>
        /// True if there is an overlap between the two locations; otherwise, false.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when either <paramref name="location"/> or <paramref name="otherLocation"/> is null.
        /// </exception>
        public static bool IsLocationOverlapping(this Location location, Location otherLocation)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (otherLocation is null)
            {
                throw new ArgumentNullException(nameof(otherLocation));
            }

            // Get the source spans of the locations
            var span1 = location.GetLineSpan();
            var span2 = otherLocation.GetLineSpan();

            // Check if the spans overlap
            return span1.StartLinePosition <= span2.EndLinePosition
                && span2.StartLinePosition <= span1.EndLinePosition;
        }

        /// <summary>
        /// Gets the index of the first parameter in the specified method symbol whose name matches any of the specified names.
        /// </summary>
        /// <param name="methodSymbol">The method symbol to search.</param>
        /// <param name="namesToMatch">The parameter names to match.</param>
        /// <returns>
        /// The index of the first matching parameter, or -1 if no match is found.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="methodSymbol"/> or <paramref name="namesToMatch"/> is null.
        /// </exception>
        public static int GetParameterSymbolIndexByName(this IMethodSymbol methodSymbol, params string[] namesToMatch)
        {
            if (methodSymbol is null)
            {
                throw new ArgumentNullException(nameof(methodSymbol));
            }

            if (namesToMatch is null)
            {
                throw new ArgumentNullException(nameof(namesToMatch));
            }

            var matchingParameter = methodSymbol.Parameters.FirstOrDefault(parameter => namesToMatch.Contains(parameter.Name));
            if (matchingParameter == null)
            {
                return -1;
            }

            return methodSymbol.Parameters.IndexOf(matchingParameter);
        }
    }
}
