import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommentaryComponent } from './commentary.component';

describe('CommentaryComponent', () => {
  let component: CommentaryComponent;
  let fixture: ComponentFixture<CommentaryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentaryComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CommentaryComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
