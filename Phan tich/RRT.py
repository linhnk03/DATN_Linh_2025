import numpy as np
import matplotlib.pyplot as plt
import random
from typing import Tuple, List
class RRTAlgorithm:
    def __init__(self, target_radius: float = 0.1, exploration_bias: float = 0.0, delta: float = 0.1, max_nodes: int = 1000):
        """
        @brief Initializes the RRT object
        @param target_radius Radius of vicinity around the target point
        @param exploration_bias Probability that the target point will be sampled as a random point
        @param delta Distance by which the tree is expanded at each step
        @param max_nodes Maximum number of nodes the RRT will search till
        """
        self.target_radius = target_radius
        self.exploration_bias = exploration_bias
        self.delta = delta
        self.max_nodes = max_nodes
        self.nodes = []

    def distance(self, point1: np.ndarray, point2: np.ndarray) -> float:
        """ 
        @brief Computes the Euclidean distance between two points
        """
        return np.linalg.norm(point1 - point2)

    def get_random_point(self, target_point: np.ndarray) -> np.ndarray:
        """ 
        @brief Samples a random point in the space
        """
        if random.random() < self.exploration_bias:
            return target_point
        else:
            return np.random.rand(2)  # Random point in [0, 1] x [0, 1]

    def run(self, source_point: Tuple[int, int], target_point: Tuple[int, int], visual: bool = False) -> Tuple[List[np.ndarray], float]:
        """
        @brief Runs the RRT Algorithm to find a path from source to target
        @param source_point Starting point (x, y)
        @param target_point Target point (x, y)
        @param visual Whether to visualize the process
        @return discovered_path, path_distance
        """
        source_point = np.array(source_point)
        target_point = np.array(target_point)

        self.nodes.append(source_point)

        if visual:
            plt.ion()
            fig, ax = plt.subplots(figsize=(10, 6))
            ax.set_xlim(0, 1)
            ax.set_ylim(0, 1)
            ax.scatter(source_point[0], source_point[1], c='green', s=15)
            ax.scatter(target_point[0], target_point[1], c='yellow', s=15)
            target_circle = plt.Circle(target_point, self.target_radius, color='r', ls='--', fill=False)
            ax.add_patch(target_circle)

        new_node = None
        while (new_node is None) or (self.distance(self.nodes[new_node], target_point) > self.target_radius):
            rand_point = self.get_random_point(target_point)
            nearest_node = np.argmin([self.distance(rand_point, node) for node in self.nodes])
            nearest_point = self.nodes[nearest_node]

            direction = rand_point - nearest_point
            direction /= np.linalg.norm(direction)  # Normalize direction
            new_point = nearest_point + direction * self.delta

            if visual:
                ax.plot([nearest_point[0], rand_point[0]], [nearest_point[1], rand_point[1]], color='blue', ls='--')
                ax.scatter(new_point[0], new_point[1], c='blue', s=15)

            # Check if the new point is within the bounds [0, 1]
            if 0 <= new_point[0] <= 1 and 0 <= new_point[1] <= 1:
                self.nodes.append(new_point)
                new_node = len(self.nodes) - 1

                if self.distance(new_point, target_point) <= self.target_radius:
                    self.nodes.append(target_point)
                    break

            if len(self.nodes) > self.max_nodes:
                print("Max number of nodes reached, could not find the path yet.")
                return [], 0

        # Backtrack to find the path
        path = [self.nodes[-1]]
        current_node = len(self.nodes) - 1

        while current_node != 0:
            current_node = 0  # Simplified: always go back to the first node
            path.append(self.nodes[current_node])

        path.reverse()
        
        if visual:
            for i in range(len(path) - 1):
                ax.plot([path[i][0], path[i + 1][0]], [path[i][1], path[i + 1][1]], c='cyan', lw=2)

            plt.ioff()
            plt.show()

        path_distance = sum(self.distance(path[i], path[i + 1]) for i in range(len(path) - 1))
        return path, path_distance

if __name__ == "__main__":
    rrt = RRTAlgorithm(target_radius=0.1, delta=0.1, exploration_bias=0.4, max_nodes=500)
    discovered_path, path_distance = rrt.run(source_point=(0.1, 0.1), target_point=(0.9, 0.9), visual=True)
    print(f'Discovered Path: {discovered_path}')
    print(f'Path Distance: {path_distance}')