import sys
import os
import subprocess
from distutils.dir_util import copy_tree
from xml.dom import minidom

print("Running with args: %s" % sys.argv)
# set "pckg_path=%cd%"
origin_dir = os.getcwd()
template_project_tests_dir = "C:/gitlab-perf-runner/project-template/Assets/Unity.PerformanceTesting.Tests"
template_project_dir = "C:/gitlab-perf-runner/project-template/"
temp_project_dir = "C:/gitlab-perf-runner/temp-project"
temp_proj_package_dir = "C:/gitlab-perf-runner/temp-project/Assets/Unity.PerformanceTesting/"
results_xml_path = origin_dir + "/results.xml"
player_log_path = origin_dir + "/player.log"
print("Origin directory: %s" % origin_dir)
# cd C:\gitlab-perf-runner\project-template\Assets\Unity.PerformanceTesting.Tests
os.chdir(template_project_tests_dir)
print("Current working directory: %s" % os.getcwd())
# git pull
print("git pull")
output = subprocess.check_output(["git", "pull"])
print(output)
# cd pckg_path
os.chdir(origin_dir)
# mkdir C:\gitlab-perf-runner\temp-project
os.mkdir(temp_project_dir)
# robocopy C:\gitlab-perf-runner\project-template C:\gitlab-perf-runner\temp-project /e /nc /ns /nfl /ndl /np
copy_tree(template_project_dir, temp_project_dir)
# robocopy %1 C:\gitlab-perf-runner\temp-project\Assets\Unity.PerformanceTesting\ /e /nc /ns /nfl /ndl /np
copy_tree(origin_dir, temp_proj_package_dir)
# start /WAIT %2 -projectpath C:\gitlab-perf-runner\temp-project -batchmode -nographics -silentcrashes -logfile %pckg_path%/player.log -runtests -testresults %pckg_path%/results.xml -testPlatform %3
run_tests_cmd = ["start", "/WAIT", sys.argv[1], "-projectpath", temp_project_dir, "-batchmode", "-nographics",
                 "-silentcrashes", "-logfile", player_log_path, "-runtests", "-testresults", results_xml_path,
                 "-testPlatform", sys.argv[2]]
print(run_tests_cmd)
output = subprocess.check_output(run_tests_cmd, shell=True)
print(output)

doc = minidom.parse(results_xml_path)
suites = doc.getElementsByTagName("test-suite")

test_cases = doc.getElementsByTagName('test-case')
passed = 0
failed = 0
skipped = 0
inconclusive = 0

failed_tests = []
for test_case in test_cases:
    result = test_case.getAttribute("result")
    name = test_case.getAttribute("name")
    if result == "Failed":
        failed += 1
        stack_trace = test_case.getElementsByTagName('stack-trace')[0].firstChild.nodeValue
        message = test_case.getElementsByTagName('message')[0].firstChild.nodeValue
        failed_tests.append((name, message, stack_trace))
    elif result == "Passed":
        passed += 1
    elif result == "Skipped":
        skipped += 1
    elif result == "Inconclusive":
        inconclusive += 1

print("---\n")
print("Test results:")
print("  Total: %s" % len(test_cases))
print("  Passed: %s" % passed)
print("  Failed: %s" % failed)
print("  Skipped: %s" % skipped)
print("  Inconclusive: %s\n" % inconclusive)

if len(failed_tests) > 0:
    print("Failed tests:\n")
    for failed_test in failed_tests:
        print("%s:\nMessage: %s\nStack trace: %s" % failed_test)

if int(failed) > 0 or int(inconclusive) > 0:
    sys.exit("ERROR: A test has failed or was inconclusive.")
