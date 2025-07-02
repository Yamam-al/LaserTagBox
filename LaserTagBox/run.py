import subprocess

# Pfad zur ausführbaren Datei
exe_path = r"C:/Users/alsho/RiderProjects/LaserTagBox/LaserTagBox/bin/Debug/net8.0/LaserTagBox.exe"

# Anzahl der Durchläufe
num_runs = 1000

for i in range(num_runs):
    print(f"Run {i+1}/{num_runs}")
    result = subprocess.Popen(exe_path)
    result.wait()
    # Ausgabe anzeigen oder loggen
    print("Exit code:", result.returncode)
    if result.stdout:
        print("Output:", result.stdout.strip())
    if result.stderr:
        print("Error:", result.stderr.strip())
