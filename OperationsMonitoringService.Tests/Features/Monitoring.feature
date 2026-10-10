Feature: Telemetry ingestion and active fever alerts
  As a ranch operator
  I want valid device readings to be stored and fever alerts to be queryable
  So that I can identify cattle requiring attention

  # Acceptance specification for service C. These scenarios are not bound to
  # a Gherkin runner yet; executable coverage currently resides in xUnit tests.

  Scenario: A high-temperature reading creates one fever alert
    Given a telemetry reading for an animal with a body temperature of 40.2 C
    When I submit the reading to POST /api/v1/telemetry
    Then the response status is 201
    And the reading is stored with its capture and receipt times
    And one active fever alert is created for that animal

  Scenario: An identical retry does not duplicate the reading or alert
    Given a valid high-temperature reading has already been accepted
    When I submit the same device identifier and capture time with identical values
    Then the response status is 200
    And the original reading identifier is returned
    And no additional alert is created

  Scenario: Conflicting values for the same reading key are rejected
    Given a reading has been accepted for a device and capture time
    When I submit different values for the same device and capture time
    Then the response status is 409
    And the original reading remains unchanged

  Scenario: Fever readings within six hours update the active alert
    Given an active fever alert was opened less than six hours ago
    When another high-temperature reading for the same animal is accepted
    Then no second fever alert is created
    And the existing alert reflects the new reading

  Scenario: Active alerts can be queried for one animal
    Given a high-temperature reading has created an active fever alert
    When I request GET /api/v1/alerts for that animal
    Then the response status is 200
    And the response includes that alert

  Scenario: Invalid telemetry is rejected
    Given a telemetry request is missing required identifiers and capture time
    When I submit the request to POST /api/v1/telemetry
    Then the response status is 400
    And no reading is stored
