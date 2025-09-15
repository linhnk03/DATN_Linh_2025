import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import os

# Đường dẫn chứa các file CSV
base_path = 'C:\\Learn\\DoAnTN\\DoAnDrone\\Assets\\Resources\\'

# ======== Hàm tính khoảng cách min và số lượng cặp < 2m ============
def compute_min_distance_from_files(prefix_local, prefix_no, num_runs=5):
    all_min_local, all_min_no = [], []
    all_close_counts_local, all_close_counts_no = [], []
    time_ref = None

    for i in range(num_runs):
        try:
            file_local = os.path.join(base_path, f'drone_profiler_coords_stateslocal{i}.csv')
            file_no = os.path.join(base_path, f'drone_profiler_coords_states{i}.csv')

            if not os.path.exists(file_local) or not os.path.exists(file_no):
                print(f"[Warning] Missing file pair: local{i} or no{i}")
                continue

            df_local = pd.read_csv(file_local)
            df_no = pd.read_csv(file_no)

            n = max(len(df_local), len(df_no))
            time_local = df_local['time'].values
            time_no = df_no['time'].values
            time = time_local if len(time_local) >= len(time_no) else time_no
            if time_ref is None:
                time_ref = time

            def pad_df(df, target_len):
                if len(df) < target_len:
                    last_row = df.iloc[-1:]
                    pad_rows = pd.concat([last_row] * (target_len - len(df)), ignore_index=True)
                    return pd.concat([df, pad_rows], ignore_index=True)
                return df

            df_local = pad_df(df_local, n)
            df_no = pad_df(df_no, n)

            pos_cols = [c for c in df_local.columns if any(c.endswith(s) for s in ['_x', '_y', '_z'])]
            pos_cols_sorted = sorted(pos_cols, key=lambda x: (int(x[1:].split('_')[0]), x[-1]))
            num_drones = len(pos_cols_sorted) // 3

            pos_local = df_local[pos_cols_sorted].values.reshape(-1, num_drones, 3)
            pos_no = df_no[pos_cols_sorted].values.reshape(-1, num_drones, 3)

            def compute_stats(positions):
                mins, close_counts = [], []
                for sample in positions:
                    d = np.linalg.norm(sample[:, None, :] - sample[None, :, :], axis=2)
                    pairwise = d[np.triu_indices(num_drones, k=1)]
                    mins.append(pairwise.min())
                    close_counts.append(np.sum(pairwise < 2.0))
                return np.array(mins), np.array(close_counts)

            min_l, count_l = compute_stats(pos_local)
            min_n, count_n = compute_stats(pos_no)

            all_min_local.append(min_l)
            all_min_no.append(min_n)
            all_close_counts_local.append(count_l)
            all_close_counts_no.append(count_n)

        except Exception as e:
            print(f"[Error] Failed to process run {i}: {e}")
            continue

    if len(all_min_local) == 0 or len(all_min_no) == 0:
        raise ValueError("Không có dữ liệu hợp lệ để tính khoảng cách min.")

    max_len = max(len(m) for m in all_min_local + all_min_no)

    def pad(arr, target_len):
        return np.concatenate([arr, np.full(target_len - len(arr), arr[-1])]) if len(arr) < target_len else arr

    all_min_local = [pad(m, max_len) for m in all_min_local]
    all_min_no = [pad(m, max_len) for m in all_min_no]
    all_close_counts_local = [pad(m, max_len) for m in all_close_counts_local]
    all_close_counts_no = [pad(m, max_len) for m in all_close_counts_no]

    if len(time_ref) < max_len:
        time_step = time_ref[1] - time_ref[0]
        extra = np.array([time_ref[-1] + time_step * (i + 1) for i in range(max_len - len(time_ref))])
        time_ref = np.concatenate([time_ref, extra])

    return (
        time_ref,
        np.mean(all_min_local, axis=0),
        np.mean(all_min_no, axis=0),
        np.mean(all_close_counts_local, axis=0),
        np.mean(all_close_counts_no, axis=0)
    )

# ======= Hàm tính quãng đường từng drone ============
def compute_average_distance_per_drone(prefix, num_runs=5):
    all_distances = []

    for i in range(num_runs):
        file_path = os.path.join(base_path, f'drone_profiler_coords_states{prefix}{i}.csv')
        if not os.path.exists(file_path):
            print(f"[Warning] Missing file: {prefix}{i}")
            continue

        df = pd.read_csv(file_path)
        pos_cols = [c for c in df.columns if any(c.endswith(s) for s in ['_x', '_y', '_z'])]
        pos_cols_sorted = sorted(pos_cols, key=lambda x: (int(x[1:].split('_')[0]), x[-1]))
        num_drones = len(pos_cols_sorted) // 3
        n_frames = len(df)

        positions = df[pos_cols_sorted].values.reshape(n_frames, num_drones, 3)

        drone_distances = np.zeros(num_drones)
        for i in range(1, n_frames):
            diffs = positions[i] - positions[i - 1]
            dists = np.linalg.norm(diffs, axis=1)
            drone_distances += dists

        all_distances.append(drone_distances)

    avg_distances = np.mean(all_distances, axis=0)
    return avg_distances

# ========= Chạy xử lý ============
time, min_local_avg, min_no_avg, close_local_avg, close_no_avg = compute_min_distance_from_files('local', '', num_runs=1)
dist_local = compute_average_distance_per_drone("local", num_runs=1)
dist_no = compute_average_distance_per_drone("", num_runs=1)

# ========= Vẽ biểu đồ 2x2 ============
fig, axs = plt.subplots(2, 2, figsize=(14, 8))

# 1. Biểu đồ khoảng cách min
axs[0, 0].plot(time, min_local_avg, label='Tối ưu cục bộ')
axs[0, 0].plot(time, min_no_avg, label='Không tối ưu')
axs[0, 0].set_title('Khoảng cách cặp nhỏ nhất theo thời gian')
axs[0, 0].set_ylabel('Min distance')
axs[0, 0].set_xlabel('Thời gian (s)')
axs[0, 0].legend()
axs[0, 0].grid(True)

# 2. Biểu đồ số lượng cặp < 2m
axs[0, 1].plot(time, close_local_avg, label='Tối ưu cục bộ')
axs[0, 1].plot(time, close_no_avg, label='Không tối ưu')
axs[0, 1].set_title('Số lượng cặp drone có khoảng cách < 2m')
axs[0, 1].set_ylabel('Số cặp < 2m')
axs[0, 1].set_xlabel('Thời gian (s)')
axs[0, 1].legend()
axs[0, 1].grid(True)

# 3. Biểu đồ quãng đường từng drone và hiển thị tổng
axs[1, 0].set_title('Quãng đường từng drone (và tổng)')
axs[1, 0].plot(dist_local, label='Tối ưu cục bộ')
axs[1, 0].plot(dist_no, label='Không tối ưu')

# Tổng quãng đường
total_local = np.sum(dist_local)
total_no = np.sum(dist_no)

axs[1, 0].set_xlabel('Drone ID')
axs[1, 0].set_ylabel('Quãng đường (m)')
axs[1, 0].legend()
axs[1, 0].grid(True)



# 4. Chỗ trống (có thể thêm biểu đồ phụ)
# 4. Thống kê tổng & trung bình quãng đường
axs[1, 1].axis('off')
mean_local = np.mean(dist_local)
mean_no = np.mean(dist_no)

text_stat = (
    f"Trung bình quãng đường / drone:\n"
    f"  - Tối ưu cục bộ: {mean_local:.2f} m\n"
    f"  - Không tối ưu: {mean_no:.2f} m\n\n"
    f"Tổng quãng đường toàn đội hình:\n"
    f"  - Tối ưu cục bộ: {total_local:.2f} m\n"
    f"  - Không tối ưu: {total_no:.2f} m"
)

axs[1, 1].text(0.05, 0.5, text_stat, fontsize=12, va='center', ha='left')

plt.tight_layout()
plt.show()
