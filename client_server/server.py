import socket
import pickle
from PIL import Image
import numpy as np

def send_all(sock, data):
    data = pickle.dumps(data)
    sock.sendall(len(data).to_bytes(4, 'big'))
    sock.sendall(data)

def receive_all(sock):
    length = int.from_bytes(sock.recv(4), 'big')
    data = b''
    while len(data) < length:
        packet = sock.recv(4096)
        if not packet:
            break
        data += packet
    return pickle.loads(data)

def edge_filter(fragment):
    Kx = np.array([[-1, 0, 1],
                   [-2, 0, 2],
                   [-1, 0, 1]])
    Ky = np.array([[-1, -2, -1],
                   [0,  0,  0],
                   [1,  2,  1]])
    gx = np.zeros_like(fragment, dtype=float)
    gy = np.zeros_like(fragment, dtype=float)

    for i in range(1, fragment.shape[0]-1):
        for j in range(1, fragment.shape[1]-1):
            region = fragment[i-1:i+2, j-1:j+2]
            gx[i, j] = np.sum(region * Kx)
            gy[i, j] = np.sum(region * Ky)

    g = np.sqrt(gx**2 + gy**2)
    g = np.clip(g, 0, 255)
    return g.astype(np.uint8)

def split_image(image):
    return [np.array(image)]

def merge_image(fragments):
    return Image.fromarray(np.vstack(fragments))

def server_main(image_path):
    image = Image.open(image_path).convert("L")
    fragments = split_image(image)

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
   
    server_socket.bind(("10.104.33.219", 2040))
    server_socket.listen(1)

    print("Serwer nasłuchuje na 10.104.33.219:2040 ...")
    client_socket, addr = server_socket.accept()
    print(f"Połączono z klientem {addr}")

    send_all(client_socket, fragments[0])
    processed_fragment = receive_all(client_socket)
    client_socket.close()
    server_socket.close()

    result = merge_image([processed_fragment])
    result.save("processed_image_server.png")
    print("Obraz przetworzony zapisany jako processed_image.png")

if __name__ == "__main__":
    server_main("input.png")
