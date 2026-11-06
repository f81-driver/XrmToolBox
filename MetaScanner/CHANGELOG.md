# Release History

## 1.0.0 (Unreleased)

Initial release of Meta Scanner, a friendlier way to browse Dataverse metadata.

### Features Added

- Table list with icons, instant filtering on display, logical and schema name or type code, quick filters (custom, system, activities, audit enabled, intersect), sorting on display or logical name and favorites per connection; the last selected table is selected again when the tool opens
- Table overview card with the key facts (type code, logical, schema and entity set name, primary columns, ownership) and color coded badges for custom/system, managed/unmanaged and audit
- Columns tab with friendly type names, details (max length, lookup targets, choice count, precision), sortable grid, type filter and helper columns hidden by default
- Column details pane with choice values and every column property
- Relationships (one-to-many, many-to-one, many-to-many), alternate keys, privileges and all table properties, each with its own filter
- Scan: search every table, column and choice value of the environment at once, and jump straight to the result
- Copy names from any row, open a table in the maker portal or its records in the browser
- Export columns and scan results to CSV
