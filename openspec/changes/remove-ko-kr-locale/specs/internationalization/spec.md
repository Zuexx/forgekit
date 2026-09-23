## Purpose

Defines which locales this starter kit ships translated content for, and what happens when
a request names a locale the kit does not support.

## ADDED Requirements

### Requirement: Supported locale set
The system SHALL support exactly two locales: `en` (default) and `zh-TW`. The system SHALL
NOT ship translated message content, a locale switcher entry, or routing support for any
other locale.

#### Scenario: Locale switcher offers only supported locales
- **WHEN** a user opens the locale switcher
- **THEN** it lists exactly `en` and `zh-TW`, in the order the routing configuration
  declares them, with no other locale present

### Requirement: Unsupported locale segment returns not found
A URL whose first path segment names a locale outside the supported set SHALL render the
application's not-found response, exactly as it does today for any other unrecognized
locale segment.

#### Scenario: Removed locale's URL prefix 404s
- **WHEN** a request's first path segment is `ko-KR` (or any other locale outside `en`/`zh-TW`)
- **THEN** the response is the application's standard not-found page, with no page content
  rendered under that prefix

### Requirement: No dangling references to a removed locale
Removing a locale from the supported set SHALL remove every reference to it from shipped
code, translated content, and documentation — a fork inspecting the source SHALL NOT find
message files, switcher labels, or documented locale lists for a locale the kit no longer
supports.

#### Scenario: No orphaned message files
- **WHEN** a locale is removed from the supported set
- **THEN** its message directory under `messages/` no longer exists in the repository
