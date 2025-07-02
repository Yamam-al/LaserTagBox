import subprocess

exe_path = r"C:/projects/LaserTagBox/LaserTagBox/bin/Debug/net8.0/LaserTagBox.exe"
num_runs = 500
timeout_seconds = 60  # 1 Minuten Timeout

for i in range(num_runs):
    print(f"Run {i+1}/{num_runs}")
    try:
        # stdout/stderr werden auf PIPE gesetzt, damit sie nach Timeout noch gelesen werden können
        result = subprocess.Popen(exe_path, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        try:
            result.wait(timeout=timeout_seconds)
        except subprocess.TimeoutExpired:
            print("Timeout reached, killing process.")
            result.kill()
            print(f"Run {i+1} skipped due to timeout.")
            continue

        # Nach Beenden: Ausgaben lesen
        stdout, stderr = result.communicate(timeout=5)
        print("Exit code:", result.returncode)
        if stdout:
            print("Output:", stdout.decode('utf-8').strip())
        if stderr:
            print("Error:", stderr.decode('utf-8').strip())
    except Exception as e:
        print(f"Exception in run {i+1}: {e}")
