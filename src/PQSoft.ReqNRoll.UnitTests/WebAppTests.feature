Feature: PQSoft.ReqNRoll API Testing

  Scenario: Create a user and retrieve it
    Given the following request
    """
    POST /api/users HTTP/1.1
    Content-Type: application/json

    {
      "name": "Alice"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json

    {
      "id": [[USER_ID]],
      "name": "Alice"
    }
    """

    Given the following request
    """
    GET /api/users/{{USER_ID}} HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/json

    {
      "id": "{{USER_ID}}",
      "name": "Test User"
    }
    """

  Scenario: Create user then create order
    Given the following request
    """
    POST /api/users HTTP/1.1
    Content-Type: application/json

    {
      "name": "Bob"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[USER_ID]]
    }
    """

    Given the following request
    """
    POST /api/orders HTTP/1.1
    Content-Type: application/json

    {
      "userId": "{{USER_ID}}"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "orderId": [[ORDER_ID]],
      "userId": "{{USER_ID}}",
      "status": "pending"
    }
    """

    Then the variable 'ORDER_ID' matches '^ORDER\d+$'
    And the variable 'ORDER_ID' is of type 'String'

  Scenario: Invalid user creation returns 400
    Given the following request
    """
    POST /api/users HTTP/1.1
    Content-Type: application/json

    {
      "name": ""
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 400 BadRequest
    Content-Type: application/json

    {
      "error": "Name is required"
    }
    """

  Scenario: Non-existent user returns 404
    Given the following request
    """
    GET /api/users/NOTFOUND HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 404 NotFound
    Content-Type: application/json

    {
      "error": "User not found"
    }
    """

  Scenario: Variable type assertions
    Given the following request
    """
    GET /api/users/TEST123 HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK

    {
      "id": [[USER_ID]],
      "name": [[USER_NAME]],
      "active": [[IS_ACTIVE]],
      "created": [[CREATED_DATE]]
    }
    """

    Then the variable 'USER_ID' is of type 'String'
    And the variable 'USER_NAME' is of type 'String'
    And the variable 'IS_ACTIVE' is of type 'Boolean'
    And the variable 'CREATED_DATE' is of type 'Date'
    And the variable 'USER_ID' is equals to 'TEST123'

  Scenario: Content-Type with charset parameter
    Given the following request
    """
    POST /api/users HTTP/1.1
    Content-Type: application/json; charset=utf-8

    {
      "name": "Charlie"
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created
    Content-Type: application/json; charset=utf-8

    {
      "id": [[USER_ID]],
      "name": "Charlie"
    }
    """

  Scenario: Plain text content type
    Given the following request
    """
    POST /api/text HTTP/1.1
    Content-Type: text/plain

    Hello World
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: text/plain

    Echo: Hello World
    """

  Scenario: XML content type
    Given the following request
    """
    POST /api/xml HTTP/1.1
    Content-Type: application/xml

    <user><name>Dave</name></user>
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: application/xml

    <response><id>[[USER_ID]]</id></response>
    """

