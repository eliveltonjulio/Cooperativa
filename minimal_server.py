from http.server import BaseHTTPRequestHandler, HTTPServer

class Handler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith('/Cooperados/Create'):
            body = '''<!DOCTYPE html><html><head><meta charset="utf-8"><title>Cadastrar cooperado</title></head><body><h1>Cadastrar cooperado</h1><p><a href="/">Voltar para a home</a></p><form method="post" action="/Cooperados/Create"><input name="Nome" placeholder="Nome" /><br/><input name="CPF" placeholder="CPF" /><br/><input name="Email" placeholder="E-mail" /><br/><button type="submit">Salvar</button></form></body></html>'''
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.send_header('Content-Length', str(len(body.encode('utf-8'))))
            self.end_headers()
            self.wfile.write(body.encode('utf-8'))
        elif self.path.startswith('/Cooperados'):
            body = '''<!DOCTYPE html><html><head><meta charset="utf-8"><title>Cooperados</title></head><body><h1>Cooperados</h1><p><a href="/">Voltar para a home</a></p><ul><li><a href="/Cooperados">Listar cooperados</a></li><li><a href="/Cooperados/Create">Cadastrar cooperado</a></li><li><a href="/Cooperados/Edit/1">Editar cooperado</a></li><li><a href="/Cooperados/Delete/1">Excluir cooperado</a></li></ul></body></html>'''
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.send_header('Content-Length', str(len(body.encode('utf-8'))))
            self.end_headers()
            self.wfile.write(body.encode('utf-8'))
        else:
            body = '''<!DOCTYPE html><html><head><meta charset="utf-8"><title>Home - Cooperativa</title></head><body><h1>HOME ATUALIZADA - MÓDULO COOPERADOS</h1><p>Acesse rapidamente as telas de consulta, cadastro, edição e exclusão.</p><ul><li><a href="/Cooperados">Listar cooperados</a></li><li><a href="/Cooperados/Create">Cadastrar cooperado</a></li><li><a href="/Cooperados/Edit/1">Editar cooperado</a></li><li><a href="/Cooperados/Delete/1">Excluir cooperado</a></li></ul></body></html>'''
            self.send_response(200)
            self.send_header('Content-Type', 'text/html; charset=utf-8')
            self.send_header('Content-Length', str(len(body.encode('utf-8'))))
            self.end_headers()
            self.wfile.write(body.encode('utf-8'))

    def do_POST(self):
        if self.path.startswith('/Cooperados/Create'):
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b'Cooperado cadastrado com sucesso')
        else:
            self.send_response(404)
            self.end_headers()

HTTPServer(('127.0.0.1', 5330), Handler).serve_forever()
