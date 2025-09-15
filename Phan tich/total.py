import pandas as pd
import numpy as np
import matplotlib.pyplot as plt

# Paths to the two result files
file_local_opt = r'C:\Learn\DoAnTN\DoAnDrone\Assets\Resources\drone_profiler1.csv'
file_no_opt    = r'C:\Learn\DoAnTN\DoAnDrone\Assets\Resources\drone_profiler.csv'

# Read the CSV files
df_local  = pd.read_csv(file_local_opt)
df_no_opt = pd.read_csv(file_no_opt)

# Extract time arrays
t_local = df_local['time'].values
t_no    = df_no_opt['time'].values

# Align lengths
n = min(len(t_local), len(t_no))
time = t_local[:n]

# Identify position columns and number of drones
pos_cols   = [c for c in df_local.columns if c.startswith('d')]
num_drones = len(pos_cols) // 3

# Reshape and truncate positions
pos_local  = df_local[pos_cols].values.reshape(-1, num_drones, 3)[:n]
pos_no_opt = df_no_opt[pos_cols].values.reshape(-1, num_drones, 3)[:n]

# 1) Tính min/mean/max khoảng cách như trước
def compute_stats(positions):
    mins, means, maxs = [], [], []
    for sample in positions:
        d = np.linalg.norm(sample[:, None, :] - sample[None, :, :], axis=2)
        pairs = d[np.triu_indices(num_drones, k=1)]
        mins.append(pairs.min())
        means.append(pairs.mean())
        maxs.append(pairs.max())
    return np.array(mins), np.array(means), np.array(maxs)

min_local, mean_local, max_local = compute_stats(pos_local)
min_no,    mean_no,    max_no    = compute_stats(pos_no_opt)

# 2) Tính quãng đường di chuyển của mỗi drone
def compute_traveled(positions):
    # positions: (n_frames, num_drones, 3)
    traveled = np.zeros(num_drones)
    for t in range(1, positions.shape[0]):
        disp = positions[t] - positions[t-1]           # shape: (num_drones,3)
        dist = np.linalg.norm(disp, axis=1)            # per-drone step distance
        traveled += dist
    return traveled

traveled_local  = compute_traveled(pos_local)
traveled_no_opt = compute_traveled(pos_no_opt)

# In hoặc lưu kết quả quãng đường
df_travel = pd.DataFrame({
    'drone_index': np.arange(num_drones),
    'distance_with_local_opt': traveled_local,
    'distance_without_opt':   traveled_no_opt
})
print("\n=== Quãng đường di chuyển theo drone (mỗi drone) ===")
print(df_travel.to_string(index=False))

# 3) Đếm số cặp va chạm < 2m tại mỗi thời điểm
threshold = 2.0
collisions_local  = []
collisions_no_opt  = []
for samp_local, samp_no in zip(pos_local, pos_no_opt):
    # ma trận khoảng cách
    d_loc = np.linalg.norm(samp_local[:,None,:] - samp_local[None,:,:], axis=2)
    d_no  = np.linalg.norm(samp_no[:,   None,:] - samp_no[None,:,:],    axis=2)
    # đếm cặp i<j với d< threshold
    pairs = np.triu_indices(num_drones, k=1)
    collisions_local.append((d_loc[pairs]  < threshold).sum())
    collisions_no_opt.append((d_no[pairs]   < threshold).sum())

# Tạo DataFrame summary collision
df_coll = pd.DataFrame({
    'time':      time,
    'col_local': collisions_local,
    'col_noopt': collisions_no_opt
})
print("\n=== Số cặp va chạm (<2m) theo thời điểm ===")
print(df_coll.head(10).to_string(index=False))

# 4) Vẽ đồ thị so sánh collision counts
plt.figure()
plt.plot(time, collisions_local, label='Local Opt')
plt.plot(time, collisions_no_opt, label='No Opt')
plt.xlabel('Time (s)')
plt.ylabel('Number of Pairs < 2m')
plt.title('Collision Pairs (<2m) Over Time')
plt.legend()
plt.tight_layout()
plt.savefig(r'C:\Learn\DoAnTN\DoAnDrone\Assets\Resources\collision_comparison.png')
plt.show()

# 5) Lưu các kết quả ra file CSV
df_travel.to_csv(r'C:\Learn\DoAnTN\DoAnDrone\Assets\Resources\travel_distances.csv', index=False)
df_coll.to_csv(r'C:\Learn\DoAnTN\DoAnDrone\Assets\Resources\collision_counts.csv', index=False)

print("\nĐã lưu travel_distances.csv và collision_counts.csv")
