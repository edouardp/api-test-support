namespace TestSupport.HttpFile
{
    /// <summary>
    /// Provides functionality to parse HTTP headers into structured components.
    /// </summary>
    public static class HttpHeadersParser
    {
        /// <summary>
        /// Parses an HTTP header string into its name, value, and optional parameters.
        /// </summary>
        /// <param name="rawHeader">The raw HTTP header string in the format "Key: Value; param1=value1; param2=value2".</param>
        /// <returns>A ParsedHeader object containing the parsed header name, value, and optional parameters.</returns>
        /// <exception cref="ArgumentException">Thrown if the header format is invalid.</exception>
        public static ParsedHeader ParseHeader(string rawHeader)
        {
            // Locate the first occurrence of ':' which separates the header name and value.
            var separatorIndex = rawHeader.IndexOf(':');
            if (separatorIndex == -1)
            {
                throw new ArgumentException("Invalid header format, missing ':' separator.");
            }

            // Extract the header name and trim any whitespace.
            var headerName = rawHeader[..separatorIndex].Trim();

            // Ensure the header name is not empty.
            if (string.IsNullOrEmpty(headerName))
            {
                throw new ArgumentException("Invalid header format, missing key.");
            }

            // Extract the header value and trim any leading/trailing whitespace.
            var headerContent = rawHeader[(separatorIndex + 1)..].Trim();

            // Split the content into the main value and optional parameters.
            var parts = headerContent.Split(';');
            var headerValue = parts[0].Trim();

            // Dictionary to store parameter key-value pairs, ignoring case.
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < parts.Length; i++)
            {
                // Split each parameter by '=', limiting to 2 parts in case of multiple '=' signs.
                var parameterParts = parts[i].Split('=', 2);
                if (parameterParts.Length == 2)
                {
                    parameters[parameterParts[0].Trim()] = parameterParts[1].Trim();
                }
            }

            // Return a structured representation of the parsed header.
            return new ParsedHeader(headerName, headerValue, parameters);
        }
    }
}
