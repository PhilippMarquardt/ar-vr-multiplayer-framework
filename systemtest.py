import sys
import subprocess
import re
from pathlib import Path

import systemtest_parser

# --- Settings -----------------------------------------------------------------
server_path = "./WorldSaverServer/build/systemtest/WorldSaverServer.exe"
client_path = "./WorldSaverClient/build/systemtest/WorldSaverClient.exe"
logs_path = "./logs"

build = False
run = "run" in sys.argv
test = "test" in sys.argv


# --- Build --------------------------------------------------------------------
if (not build):
    print(">Skipping build")
else:
    print(">Executing build\n")

# --- Run ----------------------------------------------------------------------
if (not run):
    print("\n>Skipping run")
else:
    print("\n>Executing run\n")

    Path(logs_path).mkdir(parents=True, exist_ok=True)

    print("  Starting Server")
    with open(logs_path + "/server.log", "w") as out:
        p0 = subprocess.Popen(server_path, stdout=out)

    print("  Starting Client 1")
    with open(logs_path + "/client1.log", "w") as out:
        p1 = subprocess.Popen(client_path, stdout=out)

    print("  Starting Client 2")
    with open(logs_path + "/client2.log", "w") as out:
        p2 = subprocess.Popen(client_path, stdout=out)

    exit_codes = [p.wait() for p in [p0, p1, p2]]
    print("  Test run completed")


# --- Assert results -----------------------------------------------------------
def scan_log(filename):
    prefix = 'Scene dump begin: '
    postfix = 'Scene dump end;'
    regex = re.compile(prefix + r'(?:(?!' + postfix + r')[\s\S])*' + postfix, re.DOTALL)

    with open(filename, "r") as log:
        # find errors
        errors = []
        for num, line in enumerate(log, 1):
            if line.startswith("ERROR"):
                errors.append("Error found in line " + str(num) + ": " + line)
        
        # find scene dumps
        log.seek(0)
        scene_dumps = regex.findall(log.read())

        # output
        print("Found " + str(len(errors)) + " errors in " + filename)
        for e in errors:
            print("    " + e)

        return (scene_dumps, len(errors))


def compare_scene_dumps(server_dump, client_dump):
    server_scene = systemtest_parser.parse_scene(server_dump)
    client_scene = systemtest_parser.parse_scene(client_dump)

    error = False
    for obj_1, obj_2 in zip(server_scene, client_scene):
        error = error or compare_obj(obj_1, obj_2)
    return error

def compare_obj(obj_1, obj_2):
    error = False
    if not obj_1 == obj_2:
        error = True
        print("##### Found object mismatch: \n")
        print(obj_1)
        print("---------- vs ----------")
        print(obj_2)
        print("")

    for child_1, child_2 in zip(obj_1.children, obj_2.children):
        error = error or compare_obj(child_1, child_2)
    return error

    
if (not test):
    print("\n>Skipping test")
else:
    print("\n>Executing test\n")

    (server_scene_dumps, n_errors_s) = scan_log(logs_path + "/server.log")
    (client1_scene_dumps, n_errors_c) = scan_log(logs_path + "/client1.log")
    (client2_scene_dumps, n_errors_c2) = scan_log(logs_path + "/client2.log")

    print("")

    # compare scene dumps
    print("Comparing " + str(len(server_scene_dumps)) + " scene dumps\n")
    n_errors_d = 0
    n = 1
    for server, client1, client2 in zip(server_scene_dumps, client1_scene_dumps, client2_scene_dumps):
        print("########### Scene dump " + str(n) + ":\n")
        n += 1

        res = compare_scene_dumps(server, client1)
        if res:
            n_errors_d += 1
            print("\n##### Found non-matching scene dump\n")

        res = compare_scene_dumps(server, client2)
        if res:
            n_errors_d += 1
            print("\n##### Found non-matching scene dump\n")

    print("\nTest finished with " + str(n_errors_c + n_errors_s + n_errors_d) + " errors")
