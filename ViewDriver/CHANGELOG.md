# Release History

## 1.0.0 (Unreleased)

Initial release of View Driver, a practical way to manage Dataverse personal views.

### Features Added

- Table list with icons, instant filtering, table criteria to combine (custom, system, activity, valid for Advanced Find, public, not intersect, deprecated) with a count of the tables they hide, sorting on display or logical name, favorites and the last selected table per connection
- Personal views of the selected table, with a filter and a toggle to show system views too
- New views from a template: built-in Default and Trace templates, and your own templates, made from an open view or edited in Manage templates; templates hold the primary name and system columns, so they work on every table
- Round widths: tidy all column widths at once (multiples of 25 below 200, of 50 up to the maximum of 500)
- Personal views of other users, by impersonation (needs the "Act on Behalf of Another User" privilege)
- Data preview that runs the view query as you edit it, with paging, record counts, CSV export and double-click to open a record
- Columns: drag and drop to add, reorder and remove, including columns of related tables (many-to-one), and widths in pixels; columns can also be reordered and resized in the data preview
- Filter: conditions with friendly operators per column type and choice values from the metadata; nested filters and filters on related tables are kept as they are
- Sorting on one or more columns
- FetchXML: edit and apply the query directly, or send it to FetchXML Builder and return it; columns added there show up in the view
- New, Save, Revert and Delete for personal views, with a warning before unsaved changes are lost
- Clone any view, system views included, into a personal view of yourself or another user
- Share a personal view with one or more users and teams, choose their access (read, write, delete, share, assign), and change or stop sharing
- Open a view or a record in the browser
