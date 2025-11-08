using CivicWatch.Api.Data;
using CivicWatch.Api.Models;
using CivicWatch.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq; // Adicionar o using System.Linq

namespace CivicWatch.Api.Services
{
    public class FornecedorService : IFornecedorService
    {
        private readonly ApplicationDbContext _context;

        public FornecedorService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET PÚBLICO (Método existente)
        public async Task<IEnumerable<FornecedorResponseDto>> GetPublicoAsync()
        {
            return await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Select(f => new FornecedorResponseDto
                {
                    Id = f.Id,
                    Documento = f.Documento,
                    RazaoSocial = f.RazaoSocial,
                    StatusReceita = f.StatusReceita,
                    EmConformidade = f.CheckIntegridade != null && f.CheckIntegridade.EmConformidade
                })
                .ToListAsync();
        }

        // 2. CADASTRO E VALIDAÇÃO (Método existente)
        public async Task<FornecedorResponseDto> CadastrarFornecedorAsync(FornecedorCadastroDto dto)
        {
            var novoFornecedor = new Fornecedor
            {
                Documento = dto.Documento,
                RazaoSocial = dto.RazaoSocial,
            };

            _context.Fornecedores.Add(novoFornecedor);
            await _context.SaveChangesAsync();

            bool isConforme = !dto.Documento.EndsWith("000"); 

            var check = new CheckIntegridade
            {
                FornecedorId = novoFornecedor.Id,
                DataUltimaVerificacao = DateTime.UtcNow,
                StatusReceitaWs = isConforme ? "ATIVA" : "SUSPENSA",
                EmConformidade = isConforme
            };
            
            _context.ChecksIntegridade.Add(check);
            
            novoFornecedor.StatusReceita = check.StatusReceitaWs;
            await _context.SaveChangesAsync();
            
            return new FornecedorResponseDto
            {
                Id = novoFornecedor.Id,
                Documento = novoFornecedor.Documento,
                RazaoSocial = novoFornecedor.RazaoSocial,
                StatusReceita = check.StatusReceitaWs,
                EmConformidade = check.EmConformidade
            };
        }
        
        // =========================================================
        // NOVO: IMPLEMENTAÇÕES DE CRUD (Finalizando o escopo)
        // =========================================================

        // 3. GET DETALHE (Para o endpoint GET /api/fornecedores/{id})
        public async Task<FornecedorResponseDto> GetDetalheAsync(int id)
        {
            // Busca o fornecedor e projeta diretamente para o DTO
            var fornecedor = await _context.Fornecedores
                .Include(f => f.CheckIntegridade)
                .Where(f => f.Id == id)
                .Select(f => new FornecedorResponseDto
                {
                    Id = f.Id,
                    Documento = f.Documento,
                    RazaoSocial = f.RazaoSocial,
                    StatusReceita = f.StatusReceita,
                    EmConformidade = f.CheckIntegridade != null && f.CheckIntegridade.EmConformidade
                })
                .FirstOrDefaultAsync();

            if (fornecedor == null)
            {
                throw new KeyNotFoundException(); // Usado pelo Controller para retornar 404
            }
            return fornecedor;
        }

        // 4. UPDATE (Para o endpoint PUT /api/fornecedores/{id})
        public async Task UpdateFornecedorAsync(int id, FornecedorCadastroDto dto)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null)
            {
                throw new KeyNotFoundException();
            }

            // Atualiza apenas os campos permitidos
            fornecedor.RazaoSocial = dto.RazaoSocial;
            // Se o Email for necessário, adicione: fornecedor.Email = dto.Email;
            
            // Note: O Documento não é alterado, pois é chave de identificação.
            
            await _context.SaveChangesAsync();
        }

        // 5. DELETE (Para o endpoint DELETE /api/fornecedores/{id})
        public async Task DeleteFornecedorAsync(int id)
        {
            var fornecedor = await _context.Fornecedores.FindAsync(id);
            if (fornecedor == null)
            {
                throw new KeyNotFoundException();
            }

            // Nota: Em produção, você precisaria de um mecanismo de exclusão em cascata (DELETE CASCADE) 
            // ou excluir/desassociar Contratos/Despesas primeiro. Para o MVP, basta a exclusão direta.

            _context.Fornecedores.Remove(fornecedor);
            await _context.SaveChangesAsync();
        }
    }
}