﻿using System.Linq;
using System.Text;
using CatFactory.CodeFactory;
using CatFactory.Mapping;

namespace CatFactory.SqlServer
{
    public class SqlCodeBuilder : CodeBuilder
    {
        public SqlCodeBuilder()
        {
        }

        public Database Database { get; set; }

        public override string FileName
            => Database.Name;

        public override string FileExtension
            => "sql";

        public override string Code
        {
            get
            {
                var output = new StringBuilder();

                var schemas = Database.Tables.Select(item => item.Schema).Distinct().ToList();

                foreach (var schema in schemas)
                {
                    if (string.IsNullOrEmpty(schema))
                    {
                        continue;
                    }

                    output.AppendFormat("create schema {0}", schema);
                    output.AppendLine();

                    output.AppendFormat("go");
                    output.AppendLine();

                    output.AppendLine();
                }

                foreach (var table in Database.Tables)
                {
                    output.AppendFormat("create table {0}", table.GetObjectName());
                    output.AppendLine();

                    output.AppendLine("(");

                    for (var i = 0; i < table.Columns.Count; i++)
                    {
                        var column = table.Columns[i];

                        output.AppendFormat("{0}{1} {2}", Indent(1), column.GetObjectName(), column.Type);

                        if (column.Length > 0)
                        {
                            output.AppendFormat("({0})", column.Prec > 0 ? string.Format("{0}, {1}", column.Prec, column.Scale) : column.Length.ToString());
                        }

                        output.AppendFormat(" {0}", column.Nullable ? "null" : "not null");

                        if (table.Identity != null && table.Identity.Name == column.Name)
                        {
                            output.AppendFormat(" identity({0}, {1})", table.Identity.Seed, table.Identity.Increment);
                        }

                        if (i < table.Columns.Count - 1)
                        {
                            output.Append(",");
                        }

                        output.AppendLine();
                    }

                    output.AppendLine(")");
                    output.AppendLine();
                }

                foreach (var table in Database.Tables)
                {
                    if (table.PrimaryKey != null)
                    {
                        var constraintName = Database.NamingConvention.GetPrimaryKeyConstraintName(table, table.PrimaryKey.Key.ToArray());

                        output.AppendFormat("alter table {0} add constraint {1} primary key ({2})", table.GetObjectName(), constraintName, string.Join(", ", table.PrimaryKey.Key));
                        output.AppendLine();

                        output.AppendFormat("go");
                        output.AppendLine();

                        output.AppendLine();
                    }

                    foreach (var unique in table.Uniques)
                    {
                        var constraintName = Database.NamingConvention.GetUniqueConstraintName(table, unique.Key.ToArray());

                        output.AppendFormat("alter table {0} add constraint {1} unique ({2})", table.GetObjectName(), constraintName, string.Join(", ", unique.Key));
                        output.AppendLine();

                        output.AppendFormat("go");
                        output.AppendLine();
                    }
                }

                return output.ToString();
            }
        }
    }
}
