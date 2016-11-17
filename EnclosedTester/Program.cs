using HP.HPTRIM.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnclosedTester
{
    class Program
    {
        internal class PropertySetter
        {
            public string Name { get; set; }
            public Func<Record, object, bool> Setter { get; set; }
            public Func<Record, object, bool> Tester { get; set; }
            public object Value { get; set; }
        }
        static void Main(string[] args)
        {
            using (var trim = new Database())
            {
                Console.Clear();

                var recType = (RecordType)PromptForTrimObject("RecordType name or uri", trim, GetRecordType);
                if (recType == null) return;

                var container = (Record)PromptForTrimObject("Container name or uri", trim, GetRecord);
                if (container == null) return;

                var assignee = (Location)PromptForTrimObject("Assignee name or uri", trim, GetLocation);
                if (assignee == null) return;

                var enclosed = PromptForBool("Enclose record in container? (y|n)");
                if (enclosed == null) return;

                // need to attempt the different order of container, assignee, and enclosed, to see what works
                var propertySetters = new PropertySetter[]
                {
                    new PropertySetter { Name = "Container", Setter = SetContainer, Tester = TestContainer, Value = container },
                    new PropertySetter { Name = "Assignee", Setter = SetAssignee, Tester = TestAssignee, Value = assignee },
                    new PropertySetter { Name = "Enclosed", Setter = SetEnclosed, Tester = TestEnclosed, Value = enclosed },
                };

                var allSetterPermutations = new List<List<PropertySetter>>();
                GenerateAllSetterPermuations(allSetterPermutations, null, propertySetters.ToList());

                System.Diagnostics.Debug.Assert(allSetterPermutations.Count == 6); // 3! === 3 * 2 * 1

                foreach (var sequence in allSetterPermutations)
                {
                    var record = new Record(recType);
                    record.Title = System.IO.Path.GetRandomFileName();

                    // try reseting stuff maybe?
                    record.Container = null;
                    record.SetCurrentLocationAtHome();

                    bool hadErrors = false;
                    foreach (var setter in sequence)
                    {
                        Console.Write("-> {0}", setter.Name);
                        if (!setter.Setter(record, setter.Value))
                        {
                            Console.WriteLine(" --- FAILED");
                            hadErrors = true;
                            break;
                        }
                    }
                    Console.WriteLine();
                    if (hadErrors)
                    {
                        continue;
                    }

                    // test to see if things are as they should be
                    bool success = true;
                    var messageParts = new List<string>();
                    foreach (var setter in sequence)
                    {
                        var msgPrefix = $"{setter.Name}: ";
                        if (setter.Tester(record, setter.Value))
                        {
                            messageParts.Add($"{msgPrefix}correct");
                        }    else
                        {
                            success = false;
                            messageParts.Add($"{msgPrefix}WRONG");
                        }
                    }

                    Console.WriteLine("{0} >>> {1}", ((success) ? "SUCCESS" : "FAILED"), string.Join(" -> ", messageParts));
                }


                Console.ReadLine();
                

            }
        }

        private static bool SetContainer(Record record, object container)
        {
            try
            {
                record.SetContainer(container as Record, false);
                return true;
            }
            catch
            {

            }
            return false;
        }

        private static bool TestContainer(Record record, object container)
        {
            try
            {
                var containerUri = (container as Record).Uri;
                
                return record.Container.Uri.Equals(containerUri);
            }
            catch
            {

            }
            return false;
        }

        private static bool SetAssignee(Record record, object assignee)
        {
            try
            {
                record.Assignee = (assignee as Location);
                return true;
            }
            catch
            {

            }
            return false;
        }

        private static bool TestAssignee(Record record, object assignee)
        {
            try
            {
                var assigneeUri = (assignee as Location).Uri;
                return record.Assignee.Uri.Equals(assigneeUri);
            }
            catch
            {

            }
            return false;
        }

        private static bool SetEnclosed(Record record, object enclosed)
        {
            try
            {
                //var enclosedBool = (enclosed as bool?) ?? false;
                record.SetProperty(PropertyIds.RecordIsEnclosed, enclosed);
                return true;
            }
            catch
            {

            }
            return false;
        }

        private static bool TestEnclosed(Record record, object enclosed)
        {
            try
            {
                var enclosedBool = (enclosed as bool?) ?? false;
                return (record.IsEnclosed == enclosedBool);
            }
            catch
            {

            }
            return false;
        }

        private static bool? PromptForBool(string prompt)
        {
            bool? result = null;
            while (result == null)
            {
                Console.Write($"{prompt}:> ");
                var enteredValue = Console.ReadLine().ToLower();
                if (string.IsNullOrEmpty(enteredValue))
                {
                    break;
                }

                switch (enteredValue) {
                    case "y":
                        result = true;
                        break;
                    case "n":
                        result = false;
                        break;
                    default:
                        Console.WriteLine("Must be 'y' or 'n'", enteredValue);
                        break;
                }
            }
            return result;
        }

        private static TrimMainObject PromptForTrimObject(string prompt, Database trim, Func<Database, string, TrimMainObject> objectLoaderFunc)
        {
            TrimMainObject result = null;
            while (result == null)
            {
                try
                {
                    Console.Write($"{prompt}:> ");
                    var enteredValue = Console.ReadLine();
                    if (string.IsNullOrEmpty(enteredValue))
                    {
                        break;
                    }

                    result = objectLoaderFunc(trim, enteredValue);
                    if (result == null)
                    {
                        Console.WriteLine("Failed to find {0}", enteredValue);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return result;
        }

        private static Location GetLocation(Database trim, string locationNameOrUri)
        {
            Location result;
            long uri;
            if (long.TryParse(locationNameOrUri, out uri)) {
                result = new Location(trim, uri);
            } else
            {
                result = new Location(trim, locationNameOrUri);
            }
            return result;
        }

        private static RecordType GetRecordType(Database trim, string recordTypeNameOrUri)
        {
            RecordType result;
            long uri;
            if (long.TryParse(recordTypeNameOrUri, out uri))
            {
                result = new RecordType(trim, uri);
            }
            else
            {
                result = new RecordType(trim, recordTypeNameOrUri);
            }
            return result;
        }
        
        private static Record GetRecord(Database trim, string recordNumberOrUri)
        {
            Record result;
            long uri;
            if (long.TryParse(recordNumberOrUri, out uri))
            {
                result = new Record(trim, uri);
            }
            else
            {
                result = new Record(trim, recordNumberOrUri);
            }
            return result;
        }

        private static void GenerateAllSetterPermuations(List<List<PropertySetter>> outerList, List<PropertySetter> currentList, List<PropertySetter> remainingSetters)
        {
            if ((remainingSetters == null) || (remainingSetters.Count == 0))
            {
                return;
            }
            if(currentList == null)
            {
                currentList = new List<PropertySetter>();
            }
            if(remainingSetters.Count == 1)
            {
                currentList.Add(remainingSetters[0]);
                outerList.Add(currentList);
                return;
            }

            foreach(var setter in remainingSetters)
            {
                var newRemainder = remainingSetters.ToList();
                newRemainder.Remove(setter);
                var newCurrentList = currentList.ToList();
                newCurrentList.Add(setter);
                GenerateAllSetterPermuations(outerList, newCurrentList, newRemainder);
            }

        }
    }
}
