import socket
import pickle
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

def client_main():
    client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    client_socket.connect(("10.104.33.219", 2040))

    fragment = receive_all(client_socket)
    print("Fragment odebrany, przetwarzanie...")
    processed = edge_filter(fragment)
    send_all(client_socket, processed)

    client_socket.close()
    print("Przetworzony fragment odesłany do serwera!")

if __name__ == "__main__":
    client_main()