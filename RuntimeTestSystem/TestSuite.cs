using ReportApp.Console;
using ReportApp.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

/// <summary>
/// RunTimeTestSystem
/// - used to run tests that will run during runtime main application
/// </summary>
namespace ReportApp.RuntimeTestSystem
{
     public class RunTimeTester
     {
          #region Fields

          private static RunTimeTester _instance;

          private ConsoleSystem consoleSystem = ConsoleSystem.ConsoleSystemInstance;

          private List<TestSuite> suites = new List<TestSuite>();

          #endregion Fields

          #region Properties

          public static RunTimeTester Instance
          {
               get
               {
                    if (_instance == null) {
                         _instance = new RunTimeTester();
                    }
                    return _instance;
               }
          }

          #endregion Properties

          #region Methods

          //add all new test suites here
          public void AddAllSuites()
          {
               suites.Add(AccessEntryTest.GetSuite());
               suites.Add(ChangeDataTest.GetSuite());
               suites.Add(VehicleTest.GetSuite());
          }

          public void AddSuite(TestSuite s)
          {
               suites.Add(s);
          }

          public async Task RunSuites(object arg)
          {
               //how many times to run test
               int testRuns = 1;

               //check to see if arg given by user
               if (arg != null) {
                    string desiredSuite = arg as string;
                    desiredSuite = desiredSuite.Trim();

                    var suiteQuery = suites.Where(x => x.Name == desiredSuite);
                    if (suiteQuery.Any()) {
                         for (int i = 0; i < testRuns; i++) {
                              consoleSystem.WriteLine($"Test Run #{i} of {testRuns}");
                              await RunSuite(suiteQuery.First());
                         }
                    } else {
                         consoleSystem.WriteLine($"Could not find suite named {desiredSuite}");
                         return;
                    }
               }
               //else no argument given - run all suites
               else {
                    if (testRuns > 1) {
                         consoleSystem.WriteLine("Multiple Suite Run Starting\n\n");
                    }

                    for (int i = 0; i < testRuns; i++) {
                         consoleSystem.WriteLine("Running all test suites...");

                         foreach (var s in suites) {
                              await RunSuite(s);
                         }
                         consoleSystem.WriteLine("All test suites done...");
                    }

                    if (testRuns > 1) {
                         consoleSystem.WriteLine("\n\n\nMultiple Suite Run Ended");
                    }
               }
          }

          public void RunCleanup()
          {
               consoleSystem.WriteLine("Running test cleanup...");
               foreach (var suite in suites) {
                    suite.CleanupFunction?.Invoke();
               }
               consoleSystem.WriteLine("Running test cleanup...DONE");
          }

          public string GetAllSuiteNames()
          {
               string returnString = "List of test suites:\n";

               foreach (var v in suites) {
                    returnString += v.Name + "\n";
               }

               return returnString;
          }

          private async Task RunSuite(TestSuite s)
          {
               consoleSystem.WriteLine("Running suite: " + s.Name);
               await s.RunTests();
          }

          #endregion Methods
     }

     public class Test
     {
          #region Properties

          public string Name { get; set; }
          public bool Passed { get; set; } = false;
          public bool Ran { get; set; } = false;
          public Func<Task<bool>> TestMethod { get; set; }

          #endregion Properties
     }

     /// <summary>
     /// TestSuite holds and runs all run time tests
     /// </summary>
     public class TestSuite
     {
          #region Fields

          private List<Test> testList = new List<Test>();

          #endregion Fields

          #region Properties

          public string Name { get; set; }

          public Action CleanupFunction { get; set; }

          #endregion Properties

          #region Methods

          public static void WriteLine(string line)
          {
               DispatcherHelper.GetDispatcher().Invoke(new Action(() => {
                    ConsoleSystem consoleSystem = ConsoleSystem.ConsoleSystemInstance;
                    consoleSystem.WriteLine(line);
               }));
          }

          public void AddTest(Test test)
          {
               testList.Add(test);
          }

          public async Task RunTests()
          {
               WriteLine($"Running Tests - Suite {Name}");
               WriteLine("===============================");

               foreach (var t in testList) {
                    try {
                         if (t.TestMethod == null) {
                              WriteLine($"{t.Name}: TestMethod not assigned");
                              t.Passed = false;
                              continue;
                         }

                         bool result = await t.TestMethod();
                         t.Passed = result;
                         t.Ran = true;
                         var outText = $"Method: {t.Name} Passed?: {t.Passed}";

                         WriteLine(outText);
                    }
                    catch (TestException) {
                         WriteLine($"Test:{t.Name} failed");
                    }
               }
          }

          #endregion Methods
     }

     [Serializable]
     public class TestException : Exception
     {
          #region Constructors

          public TestException()
          {
          }

          public TestException(string message) : base(message)
          {
          }

          public TestException(string message, Exception innerException) : base(message, innerException)
          {
          }

          protected TestException(SerializationInfo info, StreamingContext context) : base(info, context)
          {
          }

          #endregion Constructors
     }

     public class Assert
     {
          #region Methods

          internal static void IsTrue(bool v)
          {
               if (v == false) {
                    throw new TestException("Is not true");
               }
          }

          #endregion Methods
     }
}