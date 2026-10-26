#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <d3d11.h>
#include <d3dcompiler.h>
#include <wrl/client.h>
#include <cmath>
#include <cstdio>
#include <cstring>
#include <stdexcept>
#include "gpu/shaders.h"
using Microsoft::WRL::ComPtr;
void Check(HRESULT hr){if(FAILED(hr))throw std::runtime_error("D3D test failed: "+std::to_string(unsigned(hr)));}
ComPtr<ID3DBlob> Compile(const std::string& s,const char* profile){ComPtr<ID3DBlob>b,e;auto hr=D3DCompile(s.data(),s.size(),nullptr,nullptr,nullptr,"main",profile,0,0,&b,&e);if(e)std::puts((char*)e->GetBufferPointer());Check(hr);return b;}
int main(){try{
 ComPtr<ID3D11Device>d;ComPtr<ID3D11DeviceContext>c;
 Check(D3D11CreateDevice(nullptr,D3D_DRIVER_TYPE_WARP,nullptr,0,nullptr,0,D3D11_SDK_VERSION,&d,nullptr,&c));
 auto gsSource=gpu::PointHlsl();auto gsCode=Compile(gsSource,"gs_5_0");
 auto vsSource=gsSource.substr(0,gsSource.find("[maxvertexcount"))+"VOut main(uint id:SV_VertexID){VOut o=(VOut)0;o.pos=float4(0,0,0.5,1);o.pointSize=10;return o;}";
 auto vsCode=Compile(vsSource,"vs_5_0");ComPtr<ID3D11VertexShader>vs;Check(d->CreateVertexShader(vsCode->GetBufferPointer(),vsCode->GetBufferSize(),nullptr,&vs));
 D3D11_SO_DECLARATION_ENTRY decl{0,"SV_Position",0,0,4,0};UINT stride=16;
 ComPtr<ID3D11GeometryShader>gs;Check(d->CreateGeometryShaderWithStreamOutput(gsCode->GetBufferPointer(),gsCode->GetBufferSize(),&decl,1,&stride,1,D3D11_SO_NO_RASTERIZED_STREAM,nullptr,&gs));
 D3D11_BUFFER_DESC bd{};bd.ByteWidth=256;bd.BindFlags=D3D11_BIND_STREAM_OUTPUT;
 ComPtr<ID3D11Buffer>out,read,cb;Check(d->CreateBuffer(&bd,nullptr,&out));bd.BindFlags=0;bd.Usage=D3D11_USAGE_STAGING;bd.CPUAccessFlags=D3D11_CPU_ACCESS_READ;Check(d->CreateBuffer(&bd,nullptr,&read));
 bd={};bd.ByteWidth=(sizeof(gpu::DrawConstants)+15)&~15;bd.BindFlags=D3D11_BIND_CONSTANT_BUFFER;Check(d->CreateBuffer(&bd,nullptr,&cb));
 c->VSSetShader(vs.Get(),nullptr,0);c->GSSetShader(gs.Get(),nullptr,0);c->IASetPrimitiveTopology(D3D11_PRIMITIVE_TOPOLOGY_POINTLIST);
 ID3D11Buffer* constant=cb.Get();c->GSSetConstantBuffers(1,1,&constant);
 float reference[16]{};
 for(int scale=1;scale<=3;scale++){
  gpu::DrawConstants dc{};dc.surf[0]=640;dc.surf[1]=480;dc.surf[3]=float(scale);dc.point[0]=1;dc.point[1]=1;dc.point[2]=64;
  c->UpdateSubresource(cb.Get(),0,nullptr,&dc,0,0);ID3D11Buffer* target=out.Get();UINT offset=0;c->SOSetTargets(1,&target,&offset);c->Draw(1,0);c->SOSetTargets(0,nullptr,nullptr);c->CopyResource(read.Get(),out.Get());
  D3D11_MAPPED_SUBRESOURCE mapped{};Check(c->Map(read.Get(),0,D3D11_MAP_READ,0,&mapped));float v[16];std::memcpy(v,mapped.pData,sizeof(v));c->Unmap(read.Get(),0);
  if(scale==1){std::memcpy(reference,v,sizeof(v));if(std::abs(std::abs(v[0])-10.0f/640)>1e-6)throw std::runtime_error("Wrong sprite radius");}
  else for(int i=0;i<16;i++)if(std::abs(v[i]-reference[i])>1e-6)throw std::runtime_error("Sprite changes with render scale");
 }
 std::puts("PASS: actual geometry shader on WARP; sprite clip positions identical at 1x, 2x, 3x; no window created");return 0;
}catch(const std::exception& e){std::puts(e.what());return 1;}}
