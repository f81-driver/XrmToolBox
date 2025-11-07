using Formula81.XrmToolBox.Tools.AuditGoggles.Components;
using Formula81.XrmToolBox.Tools.AuditGoggles.Models;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Formula81.XrmToolBox.Tools.AuditGoggles.Mocks
{
    internal class EntityAuditListViewItemsSource : IEnumerable<EntityAudit>
    {
        private const string AccountIcon = "R0lGODlhEAAQAJEDAG9vb2pqamZmZv///yH/C1hNUCBEYXRhWE1QPD94cGFja2V0IGJlZ2luPSLvu78iIGlkPSJXNU0wTXBDZWhpSHpyZVN6TlRjemtjOWQiPz4gPHg6eG1wbWV0YSB4bWxuczp4PSJhZG9iZTpuczptZXRhLyIgeDp4bXB0az0iQWRvYmUgWE1QIENvcmUgNS4wLWMwNjEgNjQuMTQwOTQ5LCAyMDEwLzEyLzA3LTEwOjU3OjAxICAgICAgICAiPiA8cmRmOlJERiB4bWxuczpyZGY9Imh0dHA6Ly93d3cudzMub3JnLzE5OTkvMDIvMjItcmRmLXN5bnRheC1ucyMiPiA8cmRmOkRlc2NyaXB0aW9uIHJkZjphYm91dD0iIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RSZWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZVJlZiMiIHhtbG5zOnhtcD0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wLyIgeG1wTU06T3JpZ2luYWxEb2N1bWVudElEPSJ4bXAuZGlkOkM1QzM2QzkyNDFGQkUyMTE5NTQ2OTVENkJDN0UzOEVBIiB4bXBNTTpEb2N1bWVudElEPSJ4bXAuZGlkOkIzNzI1NkEyRkI0MTExRTJBNUU2RTY1MDg2REJFMkNCIiB4bXBNTTpJbnN0YW5jZUlEPSJ4bXAuaWlkOkIzNzI1NkExRkI0MTExRTJBNUU2RTY1MDg2REJFMkNCIiB4bXA6Q3JlYXRvclRvb2w9IkFkb2JlIFBob3Rvc2hvcCBDUzUuMSBXaW5kb3dzIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6Q0RGNjBGOUE0MUZCRTIxMTk1NDY5NUQ2QkM3RTM4RUEiIHN0UmVmOmRvY3VtZW50SUQ9InhtcC5kaWQ6QzVDMzZDOTI0MUZCRTIxMTk1NDY5NUQ2QkM3RTM4RUEiLz4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz4B//79/Pv6+fj39vX08/Lx8O/u7ezr6uno5+bl5OPi4eDf3t3c29rZ2NfW1dTT0tHQz87NzMvKycjHxsXEw8LBwL++vby7urm4t7a1tLOysbCvrq2sq6qpqKempaSjoqGgn56dnJuamZiXlpWUk5KRkI+OjYyLiomIh4aFhIOCgYB/fn18e3p5eHd2dXRzcnFwb25tbGtqaWhnZmVkY2JhYF9eXVxbWllYV1ZVVFNSUVBPTk1MS0pJSEdGRURDQkFAPz49PDs6OTg3NjU0MzIxMC8uLSwrKikoJyYlJCMiISAfHh0cGxoZGBcWFRQTEhEQDw4NDAsKCQgHBgUEAwIBAAAh+QQBAAADACwAAAAAEAAQAAACJpyPqavi75hosw14Y5rYVPRtFqQFwBcyHslqbbu+nUzGdHU/KlMAADs=";

        private readonly ReadOnlyCollection<EntityAudit> _entityAuditCollection;

        public EntityAuditListViewItemsSource()
        {
            var accountIconData = Convert.FromBase64String(AccountIcon);

            _entityAuditCollection = new List<EntityAudit>
            {
                new EntityAudit(new DateTime(2024, 10, 10, 15, 8, 23), "Alpha Beta",
                    new EntityLookupValue(1, Guid.NewGuid(), "Account 123", "account", "Account", accountIconData),
                    "Create",
                    ColorCombinations.EmeraldOdyssey1,
                    new List<EntityAuditDetail>
                    {
                        CreateDetail("Name", "Test", "Lorem Ipsum"),
                        CreateDetail("Foo", "000000", "1111111"),
                        CreateDetail("Bar", "4444", "3333"),
                        CreateDetail("Date", new DateTime(2024, 10, 10, 16, 44, 32), new DateTime(1999, 02, 22, 03, 19, 59)),
                        CreateDetail("OptionSetValue", null, null, new OptionSetValue(1), "Test"),
                        CreateDetail("EntityReference", null, null, null, new EntityLookupValue(1, Guid.NewGuid(), "Lookup", "account", "Account", accountIconData)),
                    }),
                new EntityAudit(DateTime.MinValue, "Gamma Delta",
                    new EntityLookupValue(1, Guid.NewGuid(), "Account 456", "account", "Account", accountIconData),
                    "Update",
                    ColorCombinations.PeachSkyline,
                    new List<EntityAuditDetail>
                    {
                        CreateDetail("Name", null, "Lorem Ipsum")
                    }),
                new EntityAudit(DateTime.MaxValue, "Epsilon Zeta",
                    new EntityLookupValue(1, Guid.NewGuid(), "Account 789", "account", "Account", accountIconData),
                    "Delete",
                    ColorCombinations.SoftSpring1,
                    new List<EntityAuditDetail>
                    {
                        CreateDetail( "Name", "Lorem Ipsum", null)
                    })
            }.AsReadOnly();
        }

        public IEnumerator<EntityAudit> GetEnumerator()
        {
            return _entityAuditCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static EntityAuditDetail CreateDetail(string name, object oldValue, object newValue)
        {
            return new EntityAuditDetail(name,
                new EntityAuditValue(oldValue, oldValue?.ToString()),
                new EntityAuditValue(newValue, newValue?.ToString()));
        }

        /*private static EntityAuditDetail CreateDetail(string name, object oldValue, string oldDisplay, object newValue, string newDisplay)
        {

        }*/

        private static EntityAuditDetail CreateDetail(string name, object oldValue, object oldDisplay, object newValue, object newDisplay)
        {
            return new EntityAuditDetail(name,
                new EntityAuditValue(oldValue, oldDisplay),
                new EntityAuditValue(newValue, newDisplay));
        }
    }
}
