# Quality Report

## QA Owner: @davidla

## Test strategy

| Test                                                                                       | Coverage                                                         | Passing                                            |
|--------------------------------------------------------------------------------------------|------------------------------------------------------------------|----------------------------------------------------|
| Load assets from the Resources folder                                                      | unit tests                                                       | passes when including callback in                  |
| Load assets using name async                                                               | only makes sense once when used in conjunction with Addressables | not-applicable (1)                                 |
| Load assets using tag async                                                                | only makes sense once when used in conjunction with Addressables | not-applicable (1)                                 |
| For all async operations, perform an on complete callback                                  | manual testing                                                   | issues creating unit tests. Intending to add later |
| Get all the addresses of assets marked with specific tag(s)                                | only makes sense once when used in conjunction with Addressables | not-applicable (1)                                 |
| Use the LoadAll to load all assets from an ICollection of addresses                        | manual testing                                                   | unit tests in progress                             |
| Use the LoadAllAsync to load all assets from an ICollection of addresses                   | manual testing                                                   | unit tests in progress                             |
| Load a scene that's included in the build settings                                         | manual testing                                                   | Cannot do this without first creating a build      |
| Load a scene that's in a loaded asset bundle                                               | unit tests                                                       | passing                                            |
| Load a scene that was marked as Addressable                                                | only makes sense once when used in conjunction with Addressables | not-applicable (1)                                 |
| Load assets from a remote bundle                                                           | manual testing                                                   | can't realistically unit test with remote server   |
| Load asset from a local bundle                                                             | unit tests                                                       | passing                                            |
| Load asset that has dependencies on other assets in other bundles that aren't loaded prior | manual testing                                                   | unit tests in progress                             |
| For all async operations, execute them via Coroutines                                      | manual testing                                                   | issues creating unit tests. Intending to add later |


