# RaceDay API Endpoint Plan

This plan covers Authentication, User Profile, Events, Categories, Event Enrolments, and Results. The implemented API in Part 2 matches this table. Items marked **(changed)** or **(added)** are deviations from the original Part 1 plan and are explained in the README.

Authentication uses JWT bearer tokens. "Role Required" is enforced by the API.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| POST | /api/auth/register | Registers a new user as an Organiser or Participant. **(changed)** Organisers must also supply a secret organiser code. | None (public) | { fullName, email, password, role, organiserCode (Organiser only) } | 201 Created - new user object (no password); 400 Bad Request - validation failed / bad organiser code; 409 Conflict - email already registered |
| POST | /api/auth/login | Authenticates a user and returns an access token. | None (public) | { email, password } | 200 OK - { token, user }; 401 Unauthorized - invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| GET | /api/users/me | Retrieves the logged-in user's own profile. | Any (logged in) | None | 200 OK - user profile object; 401 Unauthorized |
| PUT | /api/users/me | Updates the logged-in user's profile details. | Any (logged in) | { fullName, phone } | 200 OK - updated profile; 400 Bad Request - validation failed |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| GET | /api/events | Lists all events, optionally filtered by `date` or `location` query parameters. | None (public) | None | 200 OK - array of events |
| GET | /api/events/{id} | Retrieves a single event including its categories. | None (public) | None | 200 OK - event object; 404 Not Found - event does not exist |
| POST | /api/events | Creates a new event owned by the logged-in organiser. **(changed)** now includes eventType and distance. | Organiser | { eventName, eventDate, location, description, eventType (Run/Walk/Cycle), distance } | 201 Created - event object; 400 Bad Request - validation failed |
| PUT | /api/events/{id} | Updates an event owned by the logged-in organiser. **(changed)** now includes eventType and distance. | Organiser (owner) | { eventName, eventDate, location, description, eventType, distance } | 200 OK - updated event; 403 Forbidden - not the owner; 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event owned by the logged-in organiser. | Organiser (owner) | None | 204 No Content; 403 Forbidden - not the owner; 404 Not Found |
| GET | /api/events/{id}/enrolments | **(added)** Lists all enrolments across every category of the organiser's event. | Organiser (owner) | None | 200 OK - array of enrolments; 403 Forbidden; 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| GET | /api/events/{eventId}/categories | Lists all categories for a specific event. | None (public) | None | 200 OK - array of categories; 404 Not Found - event does not exist |
| POST | /api/events/{eventId}/categories | Adds a new category to an event. | Organiser (owner) | { categoryName, distance, maxParticipants, entryFee } | 201 Created - category object; 400; 403 Forbidden; 404 Not Found |
| PUT | /api/categories/{id} | Updates a category's details. | Organiser (owner) | { categoryName, distance, maxParticipants, entryFee } | 200 OK - updated category; 400; 403 Forbidden; 404 Not Found; 409 - maxParticipants below current enrolments |
| DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| POST | /api/categories/{id}/enrolments | Enrols the logged-in participant into a category. | Participant | None | 201 Created - enrolment record; 403 Forbidden; 404 Not Found - category does not exist; 409 Conflict - already enrolled or category full |
| GET | /api/users/me/enrolments | Lists all enrolments for the logged-in participant. | Participant | None | 200 OK - array of enrolments |
| GET | /api/categories/{id}/enrolments | Lists all participants enrolled in a category. | Organiser (owner) | None | 200 OK - array of enrolments; 403 Forbidden; 404 Not Found |
| DELETE | /api/enrolments/{id} | Cancels an enrolment. | Participant (owner) | None | 204 No Content; 403 Forbidden; 404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| POST | /api/enrolments/{id}/results | Captures a finish result for an enrolment. | Organiser (owner) | { finishTime (hh:mm:ss), position, status (Finished/DNF/DNS) } | 201 Created - result object; 400; 403 Forbidden; 404 Not Found; 409 Conflict - result already exists |
| GET | /api/categories/{id}/results | Retrieves the results/leaderboard for a category, ordered by position. | None (public) | None | 200 OK - array of results; 404 Not Found - category does not exist |
| PUT | /api/results/{id} | Updates a previously captured result. | Organiser (owner) | { finishTime, position, status } | 200 OK - updated result; 400; 403 Forbidden; 404 Not Found |
| GET | /api/users/me/results | **(added)** Lists the logged-in participant's own results. | Participant | None | 200 OK - array of results |
