from multiprocessing import Pool
from PIL import Image
import numpy as np

def edge_filter(fragment_array):
    Kx = np.array([[-1, 0, 1],
                   [-2, 0, 2],
                   [-1, 0, 1]])
    Ky = np.array([[-1, -2, -1],
                   [0,  0,  0],
                   [1,  2,  1]])
    gx = np.abs(np.convolve(fragment_array.flatten(), Kx.flatten(), 'same'))
    gy = np.abs(np.convolve(fragment_array.flatten(), Ky.flatten(), 'same'))
    g = np.sqrt(gx ** 2 + gy ** 2)
    g = g.reshape(fragment_array.shape)
    g = np.clip(g, 0, 255)
    return g.astype(np.uint8)

def split_image(image, n_parts):
    width, height = image.size
    part_height = height // n_parts
    fragments = []
    for i in range(n_parts):
        box = (0, i * part_height, width, (i + 1) * part_height)
        fragment = image.crop(box)
        fragments.append(np.array(fragment))
    return fragments

def merge_image(fragments):
    return Image.fromarray(np.vstack(fragments))

def main():
    image = Image.open("input.png").convert("L")
    n_processes = 4
    fragments = split_image(image, n_processes)

    with Pool(n_processes) as p:
        processed = p.map(edge_filter, fragments)

    result = merge_image(processed)
    result.save("processed_image.png")
    print("Zapisano processed_image.png")

if __name__ == "__main__":
    main()
