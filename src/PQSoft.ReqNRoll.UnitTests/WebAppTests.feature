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

  Scenario: CSV content type
    Given the following request
    """
    POST /api/csv HTTP/1.1
    Content-Type: text/csv

    name,age
    John,30
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK
    Content-Type: text/csv

    id,name,age
    [[USER_ID]],John,30
    """

  Scenario: Numeric type assertions
    Given the following request
    """
    GET /api/stats HTTP/1.1
    """

    Then the API returns the following response
    """
    HTTP/1.1 200 OK

    {
      "count": [[COUNT]],
      "price": [[PRICE]],
      "rating": [[RATING]],
      "available": [[AVAILABLE]],
      "discount": [[DISCOUNT]]
    }
    """

    Then the variable 'COUNT' is of type 'Number'
    And the variable 'COUNT' equals 42
    And the variable 'PRICE' is of type 'Number'
    And the variable 'PRICE' equals 99.99
    And the variable 'RATING' is of type 'Number'
    And the variable 'RATING' equals 4.5 with delta 0.1
    And the variable 'AVAILABLE' is of type 'Boolean'
    And the variable 'AVAILABLE' equals true
    And the variable 'DISCOUNT' is of type 'Number'
    And the variable 'DISCOUNT' equals 0.15

  Scenario: Set variables and use them in requests
    Given the variable 'USER_NAME' is set to 'TestUser'
    And the variable 'USER_AGE' is set to 25
    And the variable 'IS_PREMIUM' is set to true
    
    Given the following request
    """
    POST /api/profile HTTP/1.1
    Content-Type: application/json

    {
      "name": "{{USER_NAME}}",
      "age": {{USER_AGE}},
      "premium": {{IS_PREMIUM}}
    }
    """

    Then the API returns the following response
    """
    HTTP/1.1 201 Created

    {
      "id": [[PROFILE_ID]],
      "name": "{{USER_NAME}}",
      "age": {{USER_AGE}},
      "premium": {{IS_PREMIUM}},
      "status": "[[STATUS]]"
    }
    """

    Then the variable 'PROFILE_ID' matches '^PROFILE\d+$'
    And the variable 'STATUS' equals 'active'

  Scenario: Variable assertions with double quotes
    Given the variable "TEST_ID" is set to "ABC123"
    And the variable "TEST_COUNT" is set to 100
    And the variable "TEST_ACTIVE" is set to false
    And the variable "TEST_PRICE" is set to 2.50
    
    Then the variable "TEST_ID" is equals to "ABC123"
    And the variable "TEST_ID" is of type "String"
    And the variable "TEST_COUNT" equals 100
    And the variable "TEST_COUNT" is of type "Number"
    And the variable "TEST_ACTIVE" equals false
    And the variable "TEST_ACTIVE" is of type "Boolean"
    And the variable "TEST_PRICE" equals 2.50
    And the variable "TEST_PRICE" is of type "Number"

