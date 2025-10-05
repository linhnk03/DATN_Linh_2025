from svgpathtools import svg2paths
import numpy as np
from shapely.geometry import LineString, Point
import csv
import os
import glob
import math

def path_to_polyline(path, num_samples=2000):
    """Chuyển 1 path svgpathtools thành list điểm (polyline)"""
    return [path.point(i / num_samples) for i in range(num_samples + 1)]

def find_self_intersections(polyline):
    """Tìm tất cả các điểm tự cắt nhau trên polyline (list điểm phức)"""
    segments = []
    for i in range(len(polyline) - 1):
        seg = LineString([(polyline[i].real, polyline[i].imag), (polyline[i+1].real, polyline[i+1].imag)])
        segments.append(seg)
    intersection_points = set()
    for i in range(len(segments)):
        for j in range(i + 2, len(segments)):
            if i == 0 and j == len(segments) - 1:
                continue
            if segments[i].intersects(segments[j]):
                inter = segments[i].intersection(segments[j])
                if isinstance(inter, Point):
                    intersection_points.add((inter.x, inter.y))
                elif inter.geom_type == 'MultiPoint':
                    for pt in inter.geoms:
                        intersection_points.add((pt.x, pt.y))
    return intersection_points

def insert_intersections(polyline, intersections, eps=1e-5):
    inter_points = set(complex(x, y) for x, y in intersections)
    new_poly = []
    for i in range(len(polyline) - 1):
        seg_start = polyline[i]
        seg_end = polyline[i + 1]
        seg = [seg_start]
        on_this = []
        for pt in inter_points:
            v = np.array([seg_end.real - seg_start.real, seg_end.imag - seg_start.imag])
            u = np.array([pt.real - seg_start.real, pt.imag - seg_start.imag])
            cross = np.abs(v[0] * u[1] - v[1] * u[0])
            dot = v[0] * u[0] + v[1] * u[1]
            norm2 = v[0] * v[0] + v[1] * v[1]
            if cross < eps and 0 < dot < norm2:
                on_this.append(pt)
        on_this = sorted(on_this, key=lambda z: abs(z - seg_start))
        seg.extend(on_this)
        new_poly.extend(seg)
    new_poly.append(polyline[-1])
    out = []
    for pt in new_poly:
        if len(out) == 0 or abs(pt - out[-1]) >= eps:
            out.append(pt)
    return out

def sample_svg(svg_file, N, width, height, scale=1.0, alpha=0.7, beta=0.3):
    paths, _ = svg2paths(svg_file)
    # Nếu chỉ có 1 path, apply tự động chèn node tại điểm giao nhau
    if len(paths) == 1:
        path = paths[0]
        polyline = path_to_polyline(path, num_samples=2000)
        intersections = find_self_intersections(polyline)
        polyline_with_cross = insert_intersections(polyline, intersections)
        # Sample N điểm đều dọc contour
        dists = [abs(polyline_with_cross[i+1] - polyline_with_cross[i]) for i in range(len(polyline_with_cross) - 1)]
        total_length = sum(dists)
        samples = []
        for i in range(N):
            target = (i + 0.5) * total_length / N
            acc = 0
            for j in range(len(dists)):
                if acc + dists[j] >= target:
                    ratio = (target - acc) / dists[j]
                    pt = polyline_with_cross[j] + ratio * (polyline_with_cross[j+1] - polyline_with_cross[j])
                    samples.append(pt)
                    break
                acc += dists[j]
        normalized_points = []
        for pt in samples:
            nx = (pt.real - width / 2) * scale
            ny = -(pt.imag - height / 2) * scale + 80
            nz = 15.0
            normalized_points.append((nx, ny, nz))
        return np.array(normalized_points)
    # Trường hợp nhiều path (code cũ)
    path_lengths = [p.length() for p in paths]
    total_length = sum(path_lengths)
    curvatures = []
    for path in paths:
        num_samples = 40
        pts = [path.point(t / num_samples) for t in range(num_samples + 1)]
        angles = []
        for i in range(len(pts) - 1):
            dx = pts[i + 1].real - pts[i].real
            dy = pts[i + 1].imag - pts[i].imag
            if dx == 0 and dy == 0:
                continue
            angles.append(math.atan2(dy, dx))
        if len(angles) > 1:
            diffs = np.abs(np.diff(angles))
            diffs = np.minimum(diffs, 2 * np.pi - diffs)
            curvature = np.mean(diffs)
        else:
            curvature = 0.0
        curvatures.append(curvature)
    max_curv = max(curvatures) if max(curvatures) > 0 else 1.0
    normalized_curv = [c / max_curv for c in curvatures]
    weights = []
    for L, c in zip(path_lengths, normalized_curv):
        w = alpha * (L / total_length) + beta * c
        weights.append(w)
    sum_weights = sum(weights)
    dots_per_path = [max(1, int(N * w / sum_weights)) for w in weights]
    allocated = sum(dots_per_path)
    if allocated != N:
        diff = N - allocated
        dots_per_path[-1] += diff
    normalized_points = []
    for path, n_points in zip(paths, dots_per_path):
        for i in range(n_points):
            distance = (i + 0.5) / n_points * path.length()
            t = path.ilength(distance)
            pt = path.point(t)
            nx = (pt.real - width / 2) * scale
            ny = -(pt.imag - height / 2) * scale + 80
            nz = 15.0
            normalized_points.append((nx, ny, nz))
    return np.array(normalized_points)

def show_dots_console(dots, width=80, height=40):
    """Hiển thị dot trong console (ASCII art)"""
    if len(dots) == 0:
        print("❌ Không có dot nào để hiển thị.")
        return
    xs = [p[0] for p in dots]
    ys = [p[1] for p in dots]
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    scale_x = (max_x - min_x) / width if (max_x - min_x) != 0 else 1
    scale_y = (max_y - min_y) / height if (max_y - min_y) != 0 else 1
    grid = [[" " for _ in range(width)] for _ in range(height)]
    for x, y, _ in dots:
        gx = int((x - min_x) / scale_x)
        gy = int((y - min_y) / scale_y)
        gy = height - 1 - gy  # đảo trục y cho đúng hướng
        if 0 <= gx < width and 0 <= gy < height:
            grid[gy][gx] = "•"
    print("\n🖼️  Hiển thị dot (xem dạng ASCII):\n")
    for row in grid:
        print("".join(row))
    print("\n✅ Hoàn tất hiển thị.\n")

def process_frames(svg_files, csv_file, N=85, width=210, height=297, scale=1.0, default_color="1;0.5;1;1"):
    all_dots = []
    for svg_path in svg_files:
        dots = sample_svg(svg_path, N, width, height, scale)
        all_dots.append(dots)
    num_frames = len(svg_files)
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
    print(f"\n✅ CSV đã được tạo: {csv_file}\n")
    if len(all_dots) > 0:
        show_dots_console(all_dots[0])

if __name__ == "__main__":
    folder_path = r"D:\GameProject\DATN_UET_Linh\TestAnim"
    N = 85
    width = 210
    height = 297
    scale = 0.6
    default_color = "1;0.5;1;1"
    svg_files = sorted(glob.glob(os.path.join(folder_path, "*.svg")))
    if not svg_files:
        print("❌ Không tìm thấy file SVG nào trong folder:", folder_path)
    else:
        output_csv = os.path.join(folder_path, "NonLa.csv")
        process_frames(svg_files, output_csv, N, width, height, scale, default_color)