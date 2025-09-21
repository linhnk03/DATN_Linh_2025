from svgpathtools import svg2paths
import numpy as np
import csv
import os
import glob

def sample_svg(svg_file, N, width, height, scale=1.0):
    """Sample N dot từ SVG, phân bổ dot theo tỷ lệ chiều dài path (giữ nguyên tỷ lệ gốc)"""
    paths, _ = svg2paths(svg_file)

    # Tính tổng độ dài
    path_lengths = [p.length() for p in paths]
    total_length = sum(path_lengths)

    # Số dot chia theo tỉ lệ
    dots_per_path = []
    allocated = 0
    for i, L in enumerate(path_lengths):
        if i < len(paths) - 1:  # tất cả trừ path cuối
            n = int(N * (L / total_length))
            dots_per_path.append(n)
            allocated += n
        else:
            # path cuối nhận phần dư
            dots_per_path.append(N - allocated)

    normalized = []

    # Scale theo chiều dài lớn nhất để giữ đúng tỷ lệ
    max_dim = max(width, height)

    # Sinh dot trên từng path
    for path, n_points in zip(paths, dots_per_path):
        if n_points <= 0:
            continue

        for i in range(n_points):
            distance = (i + 0.5) / n_points * path.length()  # offset 0.5 để đều đẹp
            t = path.ilength(distance)
            pt = path.point(t)

            # Giữ nguyên tỷ lệ từ gốc SVG, dịch tâm về (0,0)
            nx = (pt.real - width / 2) * scale
            ny = -(pt.imag - height / 2) * scale + 80
            nz = 15.0
            normalized.append((nx, ny, nz))

    return np.array(normalized)


def process_frames(svg_files, csv_file, N=85, width=210, height=297, scale=1.0, default_color="1;0.5;1;1"):
    """Xuất CSV: id, color, numberFrame, coords..."""
    all_dots = []

    # Sinh dots cho từng frame
    for svg_path in svg_files:
        dots = sample_svg(svg_path, N, width, height, scale)
        all_dots.append(dots)

    num_frames = len(svg_files)

    # Xuất CSV
    with open(csv_file, "w", newline="") as f:
        writer = csv.writer(f)
        header = ["id", "color", "numberFrame"] + [f"frame{i+1}" for i in range(num_frames)]
        writer.writerow(header)

        for idx in range(N):
            row = [idx, default_color, num_frames]
            for frame_dots in all_dots:
                x, y, z = frame_dots[idx]
                row.append(f"{x:.4f};{y:.4f};{z:.4f}")
            writer.writerow(row)

    print(f"✅ CSV đã tạo: {csv_file}")


if __name__ == "__main__":
    # === Cấu hình ===
    folder_path = r"D:\GameProject\DATN_UET_Linh\TestAnim"   # 👉 đổi thành folder chứa các file SVG
    N = 85                     # số drone
    width = 210                # chiều rộng canvas SVG (theo file gốc)
    height = 297               # chiều cao canvas SVG (theo file gốc)
    scale = 0.6                # hệ số scale (giữ nguyên kích thước gốc)
    default_color = "1;0.5;1;1"  # màu mặc định RGBA

    # === Lấy danh sách file SVG ===
    svg_files = sorted(glob.glob(os.path.join(folder_path, "*.svg")))

    if not svg_files:
        print("❌ Không tìm thấy file SVG nào trong folder:", folder_path)
    else:
        output_csv = os.path.join(folder_path, "output.csv")
        process_frames(svg_files, output_csv, N, width, height, scale, default_color)
