# TODO: HTTP Spec Compliance Improvements

## Major Issues to Address

### 1. Header Parameter Parsing (RFC 9110)
**Current Issue**: Uses semicolons (`;`) for ALL headers, but HTTP spec varies by header type

**Required Changes**:
- `Accept`: `application/json;q=0.9, text/plain;q=0.8` (commas separate values)
- `Cache-Control`: `max-age=3600, must-revalidate` (commas separate directives)
- `Content-Type`: `application/json; charset=utf-8` (semicolons for parameters) ✅ Already correct

**Impact**: Currently `Accept: application/json, text/plain` treated as single value instead of two

### 2. Multiple Header Values (RFC 9110 Section 5.2)
**Current Issue**: Doesn't parse comma-separated values in headers

**Required Changes**:
- Implement header-specific parsing logic
- Parse comma-separated values for appropriate headers
- Handle quality values (q-values) correctly

**Examples**:
```http
Accept: application/json, text/plain, */*
Cache-Control: max-age=3600, must-revalidate, private
```

### 3. Quoted String Handling (RFC 9110 Section 5.6.4)
**Current Issue**: No proper quoted-string parsing with escape sequence handling

**Required Changes**:
- Implement RFC 9110 quoted-string grammar
- Handle escaped characters: `\"`, `\\`
- Proper unescaping of quoted values

**Examples**:
```csharp
Content-Disposition: attachment; filename="my \"quoted\" file.txt"
Authorization: Digest username="user", realm="Protected Area"
```

## Minor Improvements

### 4. Token Validation (RFC 9110 Section 5.6.2)
- Validate header names against token grammar
- Reject invalid characters in header names

### 5. Whitespace Handling (RFC 9110 Section 5.6.3)
- Improve OWS (optional whitespace) handling around commas
- Better trimming of parameter values

### 6. Header Field Value Validation
- Validate header values against allowed characters
- Handle obs-text (obsolete text) properly

## Implementation Strategy

1. **Phase 1**: Header-specific parsing for common headers (`Accept`, `Cache-Control`, `Content-Type`)
2. **Phase 2**: Quoted string parser with escape handling
3. **Phase 3**: Token validation and improved whitespace handling

## Test Coverage

Current tests in `ParsedHttpRequestTests.cs` document these limitations:
- `ToHttpRequestMessage_Should_Handle_Accept_Header_With_Comma_Separated_Values`
- `ToHttpRequestMessage_Should_Handle_Accept_With_Quality_And_Multiple_Values`
- `ToHttpRequestMessage_Should_Handle_Cache_Control_Comma_Separated_Directives`
- `ToHttpRequestMessage_Should_Handle_Quoted_String_In_Header_Value`
- `ToHttpRequestMessage_Should_Handle_Header_With_Escaped_Quotes`

## References

- [RFC 9110: HTTP Semantics](https://tools.ietf.org/html/rfc9110)
- [RFC 9111: HTTP Caching](https://tools.ietf.org/html/rfc9111)
- [RFC 9112: HTTP/1.1](https://tools.ietf.org/html/rfc9112)
